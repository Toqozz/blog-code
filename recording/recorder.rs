use std::{io::Write, process::Stdio, sync::mpsc::{self, Sender}, thread::{self, JoinHandle}};

struct FfmpegProcess {
    send: Sender<Vec<u8>>,
    handle: JoinHandle<()>,
}

pub struct Recorder {
    width: u32,
    height: u32,
    framerate: u32,

    recording: bool,
    wants_screenshot: bool,
    
    buffer: wgpu::Buffer,

    ffmpeg: Option<FfmpegProcess>,
}

#[allow(dead_code)]
impl Recorder {
    pub fn new(device: &wgpu::Device, width: u32, height: u32, framerate: u32) -> Self {
        let buffer = device.create_buffer(
            &wgpu::BufferDescriptor {
                label: Some("recording_buffer"),
                size: (4 * width * height) as u64,   // u32 * texture size
                usage: wgpu::BufferUsages::COPY_DST | wgpu::BufferUsages::MAP_READ,
                mapped_at_creation: false,
            }
        );
        
        Self {
            width,
            height,
            framerate,

            recording: false,
            wants_screenshot: false,

            buffer,
            ffmpeg: None,
        }
    }
    
    pub fn recording_framerate(&self) -> u32 {
        self.framerate
    }
    
    pub fn screenshot(&mut self) {
        self.wants_screenshot = true;
    }

    pub fn is_recording(&self) -> bool {
        self.recording
    }

    pub fn begin_record(&mut self) {
        self.recording = true;       

        assert!(self.ffmpeg.is_none());
        let ffmpeg = {
            let (send, recv) = mpsc::channel::<Vec<u8>>();
            let (width, height) = (self.width, self.height);
            let handle = thread::spawn(move || {
                // do ffmpeg stuff
                let size = format!("{}x{}", width, height);
                // Define the FFmpeg command and its arguments
                let ffmpeg_cmd = "ffmpeg";
                let args = [
                    "-r", "60",                     // Input frame rate
                    "-f", "rawvideo",
                    "-vcodec", "rawvideo",
                    "-pix_fmt", "bgra",
                    "-s", &size,
                    "-i", "-",                      // Input as stdin.
                    "-c:v", "libvpx-vp9",
                    "-pix_fmt", "yuva420p",
                    "-y",                           // Overwrite output file.
                    "output.webm",                  // File name
                ];

                // Spawn the FFmpeg command
                let mut child = std::process::Command::new(ffmpeg_cmd)
                    .args(&args)
                    .stdin(Stdio::piped())
                    .spawn()
                    .expect("Failed to spawn FFmpeg command");
                
                let stdin = child.stdin.as_mut().expect("Couldn't open ffmpeg stdin.");
                
                for data in recv {
                    stdin.write_all(data.as_slice()).unwrap();
                }
                
                let output = child.wait_with_output().unwrap();
                // Check the output and error messages
                if output.status.success() {
                    println!("FFmpeg command executed successfully.");
                } else {
                    // The `stderr` field of the output contains any error messages
                    let error_message = String::from_utf8_lossy(&output.stderr);
                    println!("FFmpeg command failed: {}", error_message);
                }
            });
            
            FfmpegProcess {
                send,
                handle,
            }
        };
        
        self.ffmpeg = Some(ffmpeg);
    }

    pub fn end_record(&mut self) {
        let _span = tracy_client::span!();

        self.recording = false;
        
        let ffmpeg = self.ffmpeg.take().unwrap();
        drop(ffmpeg.send);
        ffmpeg.handle.join().unwrap();
    }

    pub fn end_frame(&mut self, device: &wgpu::Device, encoder: &mut wgpu::CommandEncoder, frame_texture: &wgpu::Texture) {
        let _span0 = tracy_client::span!();

        if !self.recording && !self.wants_screenshot {
            return;
        }

        {
            let _span1 = tracy_client::span!("copy_texture_to_buffer");

            encoder.copy_texture_to_buffer(
                wgpu::ImageCopyTexture {
                    aspect: wgpu::TextureAspect::All,
                    texture: frame_texture,
                    mip_level: 0,
                    origin: wgpu::Origin3d::ZERO,
                },
                wgpu::ImageCopyBuffer {
                    buffer: &self.buffer,
                    layout: wgpu::ImageDataLayout {
                        offset: 0,
                        bytes_per_row: Some(4 * self.width),    // 4 u8s * width
                        rows_per_image: Some(self.height),
                    },
                },
                frame_texture.size(),
            );
        }

        let buffer_slice = self.buffer.slice(..);
        {
            let _span1 = tracy_client::span!("get_buffer");

            pollster::block_on(async {
                // NOTE: We have to create the mapping THEN device.poll() before await the
                // future.  Otherwise the application will freeze.
                let (tx, rx) = futures_intrusive::channel::shared::oneshot_channel();
                buffer_slice.map_async(wgpu::MapMode::Read, move |result| {
                    tx.send(result).unwrap();
                });
                device.poll(wgpu::Maintain::Wait);
                rx.receive().await.unwrap().unwrap();
            });
        }
                
        {
            let _span2 = tracy_client::span!("write_to_channel");

            // We need to drop this before unmapping.
            let data = buffer_slice.get_mapped_range();

            // Send buffer to recording thread.  We need to copy the data to do this safely.
            if self.recording {
                let ffmpeg = self.ffmpeg.as_mut().unwrap();
                // We effectively have to allocate anyway to do `send()`, so there's no point having a buffer here.
                let v = {
                    let _span3 = tracy_client::span!("recording_vec");
                    let mut vec = Vec::with_capacity(data.len());
                    vec.extend_from_slice(&data);
                    vec
                };
                ffmpeg.send.send(v).expect("Failed to send to ffmpeg thread.");
            }
            
            // Also, take a screenshot if desired.
            // TODO: different filenames.
            if self.wants_screenshot {
                let _span3 = tracy_client::span!("screenshot");
                // It's BGRA
                lodepng::encode_file(
                    "screen.png",
                    &data,
                    self.width as usize,
                    self.height as usize,
                    lodepng::ColorType::BGRA,
                    8,
                ).expect("Failed to write PNG.");
            }

            self.wants_screenshot = false;
        }

        self.buffer.unmap();
    }
}

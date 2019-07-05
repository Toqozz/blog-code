using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CoordinateMap : MonoBehaviour {
    private Sprite sprite;

    private void Start() {
        sprite = GetComponent<SpriteRenderer>().sprite;
    }

    public Vector2 TextureSpaceCoord(Vector3 worldPos) {
        float ppu = sprite.pixelsPerUnit;
        
        // Local position on the sprite in pixels.
        Vector2 localPos = transform.InverseTransformPoint(worldPos) * ppu;
        
        // When the sprite is part of an atlas, the rect defines its offset on the texture.
        // When the sprite is not part of an atlas, the rect is the same as the texture (x = 0, y = 0, width = tex.width, ...)
        var texSpacePivot = new Vector2(sprite.rect.x, sprite.rect.y) + sprite.pivot;
        Vector2 texSpaceCoord = texSpacePivot + localPos;

        return texSpaceCoord;
    }
    
    public Vector2 TextureSpaceUV(Vector3 worldPos) {
        Texture2D tex = sprite.texture;
        Vector2 texSpaceCoord = TextureSpaceCoord(worldPos);
        
        // Pixels to UV(0-1) conversion.
        Vector2 uvs = texSpaceCoord;
        uvs.x /= tex.width;
        uvs.y /= tex.height;


        return uvs;
    }
}

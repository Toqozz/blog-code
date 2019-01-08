public static class Quality {
	public enum QualityGrade {
		Junk,
		Brittle,
		Passable,
		Sturdy,
		Magical,
		Mystic,
		Unset
	}

	public static Color GradeToColor(QualityGrade grade) {
		switch (grade) {
		case QualityGrade.Mystic:
			return Color.cyan;
		case QualityGrade.Magical:
			return Color.magenta;
		case QualityGrade.Sturdy:
			return Color.green;
		case QualityGrade.Passable:
			return Color.white;
		case QualityGrade.Brittle:
			return Color.yellow;
		case QualityGrade.Junk:
			return Color.red;
		case Quality.QualityGrade.Unset:
			return Color.grey;
		default:
			return Color.red;
		}
	}

	public static string GradeToString(QualityGrade grade) {
		switch (grade) {
		case QualityGrade.Mystic:
			return "Mystic";
		case QualityGrade.Magical:
			return "Magical";
		case QualityGrade.Sturdy:
			return "Sturdy";
		case QualityGrade.Passable:
			return "Passable";
		case QualityGrade.Brittle:
			return "Brittle";
		case QualityGrade.Junk:
			return "Junk";
		case QualityGrade.Unset:
			return "Not Graded";
		default:
			return "Not Graded";
		}
	}

	public static Quality.QualityGrade CalculateCombinedQuality(QualityGrade grade1, QualityGrade grade2) {
		// We're overly fair to the player.
		// normally... -> 1 + 4 = 5 / 2 = 2.5 = 2 = Brittle.
		// we do... -> 1 + 4 + 1 = 5 /2 = 3 = 3 = Sturdy.
		return (Quality.QualityGrade)((int)((float)((int) grade1 + (int) grade2) + 1) / 2f);
	}
}

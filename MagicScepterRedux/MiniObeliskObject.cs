namespace MagicScepterRedux
{
	public class MiniObeliskObject
	{
		public string Name { get; set; }

		public int CoordX { get; set; }

		public int CoordY { get; set; }

		public MiniObeliskObject(string name, int x, int y)
		{
			Name = name;
			CoordX = x;
			CoordY = y;
		}
	}
}

namespace MagicScepterRedux	
{
	public enum WarpLocationChoice
	{
		Farm,
		Beach,
		Mountain,
		Desert,
		Island,
		IslandFarm,
		DeepWoods,
		MiniObelisk,
		None
	}
	public class WarpLocation
	{
		public string Name { get; private set; }

		public int CoordX { get; private set; }

		public int CoordY { get; private set; }

		public WarpLocation(string name, int x, int y)
		{
			Name = name;
			CoordX = x;
			CoordY = y;
		}
	}
}

using System.Collections.Generic;
using Godot;

namespace SpaceGame.Entities.Enemy
{
	public partial class EnemyLegion : Node2D
	{
		// ПЕРЕМЕННЫЕ
		[Export] public PackedScene EnemyShuttleTemplate { get; set; }
		private List<EnemyShuttle> _shuttles = new List<EnemyShuttle>();
		private EnemyShuttle _leader;
		private Vector2[] _formationOffsets = new Vector2[]
		{
			new Vector2(0, 0),        // лидер
			new Vector2(0, -200),     // сверху
			new Vector2(-200, 0),     // слева
			new Vector2(200, 0),      // справа
			new Vector2(0, 200)       // снизу
		};

		// READY
		public override void _Ready()
		{
			base._Ready();

			if (EnemyShuttleTemplate != null)
			{
				// Итерация по вражеским шаттлам
				for (int i = 0; i < _formationOffsets.Length; i++)
				{
					var shuttle = EnemyShuttleTemplate.Instantiate<EnemyShuttle>();
					AddChild(shuttle);
					shuttle.GlobalPosition = this.GlobalPosition + _formationOffsets[i];
					_shuttles.Add(shuttle);
				}
				_leader = _shuttles[0]; // первого назначаем лидером
				GD.Print($"Легион создан. Врагов: {_shuttles.Count}. Лидер: {_leader.Name}");
			} else
			{
				GD.PrintErr("Ошибка: EnemyShuttleTemplate пуст!");
				return;
			}
		}

	}
}

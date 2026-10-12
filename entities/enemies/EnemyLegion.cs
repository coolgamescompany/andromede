using System.Collections.Generic;
using Godot;
using SpaceGame.Entities.Planet;

namespace SpaceGame.Entities.Enemy
{
	public partial class EnemyLegion : Node2D
	{
		// ПЕРЕМЕННЫЕ
		[Export] public PackedScene EnemyShuttleTemplate { get; set; }

		private Vector2 _targetPosition = new Vector2(10000, 10000);
		private SpaceGame.Entities.Planet.Planet _targetPlanet = null;
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
					shuttle.Name = $"Enemy_{i}";
					AddChild(shuttle);
					shuttle.GlobalPosition = this.GlobalPosition + _formationOffsets[i];
					_shuttles.Add(shuttle);
				}
				_leader = _shuttles[0]; // первого назначаем лидером
				GD.Print($"Легион создан. Врагов: {_shuttles.Count}. Лидер: {_leader.Name}");

				_leader.ArrivedAtTarget += OnLeaderArrived;
			} else
			{
				GD.PrintErr("Ошибка: EnemyShuttleTemplate пуст!");
				return;
			}

			FindNearestUncapturedPlanet();
		}

		// PROCESS
		public override void _Process(double delta)
		{
			base._Process(delta);

			if (_leader == null) return;

			if (_targetPlanet != null && !_targetPlanet.isCaptured)
			{
				_targetPosition = _targetPlanet.GlobalPosition;
			} 
			else if (_targetPlanet != null && _targetPlanet.isCaptured)
			{
				FindNearestUncapturedPlanet();
			}

			_leader.SetTarget(_targetPosition);

			for (int i = 1; i < _shuttles.Count; i++)
			{
				Vector2 formationPos = _leader.GlobalPosition + _formationOffsets[i];
				_shuttles[i].SetTarget(formationPos);
			}
		}


		// ON LEADER ARRIVED
		private void OnLeaderArrived()
		{
			GD.Print($"Лидер {_leader.Name} долетел!");
			_leader.TryLandOnPlanet(_targetPlanet);
		}

		// FIND NEAREST UNCAPTURED PLANET
		private void FindNearestUncapturedPlanet()
		{
			var planets = GetTree().GetNodesInGroup("planets");
			SpaceGame.Entities.Planet.Planet nearest = null;
			float minDistance = float.MaxValue;

			foreach (Node node in planets)
			{
				if (node is SpaceGame.Entities.Planet.Planet planet && !planet.isCaptured && !planet.IsOccupied)
				{
					float dist = GlobalPosition.DistanceTo(planet.GlobalPosition);
					if (dist < minDistance)
					{
						minDistance = dist;
						nearest = planet;
					}
				}
			}

			_targetPlanet = nearest;

			if (nearest != null)
			{
				GD.Print($"Легион выбрал цель: {nearest.PlanetName}");
			}
			else
			{
				GD.Print("Легион: все планеты захвачены!");
			}
		}

	}
}

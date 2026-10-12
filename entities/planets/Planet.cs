using Godot;
using System;

namespace SpaceGame.Entities.Planet
{
	public partial class Planet : StaticBody2D
	{
		[Export] public string PlanetName { get; set; } = "Unknown";
		[Export] public string PlanetClass { get; set; } = "M-Class (Habitable)";
		[Export] public float PlanetRadius { get; set; } = 1.0f;
		[Export] public float PlanetMass { get; set; } = 1.0f;

		[Export] public bool isCaptured { get; set; } = false;
		[Export] public float CaptureProgress { get; private set; } = 0.0f; 
		[Export] public float CaptureSpeed { get; private set; } = 15.0f; 

		public enum CapturedBy { None, Player, Enemy }
		public CapturedBy CapturedByWhom { get; private set; } = CapturedBy.None;
		private bool _isEnemyLanding = false;

		private bool _isPlayerLanded = false;
		private RigidBody2D _shuttleBody = null;
		public bool IsOccupied => _shuttleBody != null;

		private Area2D _gravityArea;
		private bool _isPlayerInsideZone = false;

		private Line2D _atmosphereLine;
		private Color _initialPlanetColor;

		public override void _Ready()
		{
			base._Ready();

			_gravityArea = GetNode<Area2D>("%GravityArea");
			_gravityArea.BodyEntered += OnShuttleEnteredOrbit;
			_gravityArea.BodyExited += OnShuttleExitedOrbit;

			_atmosphereLine = GetNode<Line2D>("%AtmosphereLine");
			if (_atmosphereLine != null)
			{
				_initialPlanetColor = _atmosphereLine.DefaultColor;
				// Вызывает сеттер в circle.gd, строит визуал
				_atmosphereLine.Set("radius", PlanetRadius);
			}

			// ИСПРАВЛЕНИЕ: Делаем физическую коллизию УНИКАЛЬНОЙ для этой планеты
			var physicalCollision = GetNode<CollisionShape2D>("PhysicalCollision");
			if (physicalCollision != null && physicalCollision.Shape is CircleShape2D physicalCircle)
			{
				// Дублируем ресурс, чтобы изменение радиуса не ломало другие планеты!
				CircleShape2D uniqueShape = (CircleShape2D)physicalCircle.Duplicate();
				uniqueShape.Radius = PlanetRadius * 1.007f;
				physicalCollision.Shape = uniqueShape;
			}

			// ИСПРАВЛЕНИЕ: Делаем зону гравитации УНИКАЛЬНОЙ для этой планеты
			var gravityCollision = _gravityArea.GetNode<CollisionShape2D>("CollisionShape2D");
			if (gravityCollision != null && gravityCollision.Shape is CircleShape2D gravityCircle)
			{
				// Дублируем ресурс гравитационного поля
				CircleShape2D uniqueGravityShape = (CircleShape2D)gravityCircle.Duplicate();
				uniqueGravityShape.Radius = PlanetRadius * 2.6f;
				gravityCollision.Shape = uniqueGravityShape;
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);

			// Если автопилот усадил корабль на нашу поверхность
			if (_isPlayerLanded && _shuttleBody != null && !isCaptured)
			{
				// 1. Определяем, кто сейчас на планете
				CapturedBy incoming = _isEnemyLanding ? CapturedBy.Enemy : CapturedBy.Player;

				// 2. Если на планете ещё никто не хозяйничал — этот и есть хозяин
				if (CapturedByWhom == CapturedBy.None)
				{
					CapturedByWhom = incoming;
				}

				// 3. Если это тот же хозяин — прогресс растёт
				if (CapturedByWhom == incoming)
				{
					CaptureProgress += CaptureSpeed * (float)delta;
				}
				else
				{
					// 4. Если чужой — стираем прогресс предыдущего хозяина
					CaptureProgress -= CaptureSpeed * (float)delta;

					// 5. Если дошли до нуля — планета нейтральна, хозяин меняется
					if (CaptureProgress <= 0f)
					{
						CaptureProgress = 0f;
						CapturedByWhom = incoming;
					}
				}

				// 6. Цвет — от хозяина прогресса
				if (_atmosphereLine != null)
				{
					float progressFactor = CaptureProgress / 100f;
					Color targetColor = (CapturedByWhom == CapturedBy.Enemy) ? Colors.Red : Colors.Green;
					_atmosphereLine.DefaultColor = _initialPlanetColor.Lerp(targetColor, progressFactor);
				}

				// 7. Захват завершён
				if (CaptureProgress >= 100f)
				{
					CaptureProgress = 100f;
					isCaptured = true;
					OnPlanetCaptured();
				}
			}
		}


		public void OnShuttleEnteredOrbit(Node2D body)
		{
			if (body.Name == "Shuttle")
			{
				_isPlayerInsideZone = true;
				GD.Print($"Шаттл вышел на орбиту {PlanetName}");
			}
		}

		public void OnShuttleExitedOrbit(Node2D body)
		{
			if (body.Name == "Shuttle")
			{
				_isPlayerInsideZone = false;
				_isPlayerLanded = false;
				_shuttleBody = null;
				GD.Print($"Шаттл покинул орбиту планеты {PlanetName}");
			}
		}

		public void OnPlanetCaptured()
		{
			GD.Print($"Планета {PlanetName} полностью захвачена {(CapturedByWhom == CapturedBy.Enemy ? "врагом" : "игроком")}!");

			//var collisionPlanet = GetNode<CollisionShape2D>("PhysicalCollision");
			//if (collisionPlanet != null)
			//{
			//	collisionPlanet.Disabled = true;
			//}
			// Вместо Disabled = true мы просто убираем планету из физического слоя столкновений (Layer 1).
			// Теперь сквозь нее можно пролетать, и RigidBody2D шаттла больше никогда в ней не застрянет!
			CollisionLayer = 0; 
			CollisionMask = 0;
		}


		// Этот метод автоматически вызывает сам Шаттл при физическом касании
		public bool RegisterLanding(RigidBody2D shuttle, bool isEnemy)
		{
			if (_isPlayerLanded && _shuttleBody != null && _shuttleBody != shuttle)
			{
				return false;
			}

			_shuttleBody = shuttle;
			_isPlayerLanded = true;
			_isEnemyLanding = isEnemy;

			// ОТКЛЮЧАЕМ столкновение шаттла с планетой на время посадки (выключаем маску коллизии)
			// Шаттл плавно встанет на нужную дистанцию без физического выталкивания
			shuttle.CollisionMask |= 1; 

			return true;
		}

		public void ReleaseLanding()
		{
			if (_shuttleBody != null)
			{
				// ВОЗВРАЩАЕМ коллизию шаттлу обратно при взлете
				_shuttleBody.CollisionMask |= 1;
			}
			_isPlayerLanded = false;
			_shuttleBody = null;
		}

	}
}

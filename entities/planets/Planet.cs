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
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);

			// Если автопилот усадил корабль на нашу поверхность
			if (_isPlayerLanded && _shuttleBody != null && !isCaptured)
			{
				CaptureProgress += CaptureSpeed * (float)delta;
				GD.Print($"Шаттл пристыкован. Идет захват {PlanetName}: {Mathf.Round(CaptureProgress)}%");

				if (_atmosphereLine != null)
				{
					float progressFactor = CaptureProgress / 100f;
					Color targetColor = _isEnemyLanding ? Colors.Red : Colors.Green;
					_atmosphereLine.DefaultColor = _initialPlanetColor.Lerp(targetColor, progressFactor);
				}

				if (CaptureProgress >= 100f)
				{
					CaptureProgress = 100f;
					isCaptured = true;
					CapturedByWhom = _isEnemyLanding ? CapturedBy.Enemy : CapturedBy.Player;
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

			var collisionPlanet = GetNode<CollisionShape2D>("PhysicalCollision");
			if (collisionPlanet != null)
			{
				collisionPlanet.Disabled = true;
			}
		}


		// Этот метод автоматически вызывает сам Шаттл при физическом касании
		public void RegisterLanding(RigidBody2D shuttle, bool isEnemy)
		{
			// Если кто-то уже сидит на планете — отказываем
			if (_isPlayerLanded && _shuttleBody != null && _shuttleBody != shuttle)
			{
				GD.Print($"Планета {PlanetName} уже занята другим шаттлом. Посадка отклонена.");
				return;
			}

			_shuttleBody = shuttle;
			_isPlayerLanded = true;
			_isEnemyLanding = isEnemy;
		}

		// Вызывается, когда шаттл покидает планету (например, автопилот переключился на другую цель)
		public void ReleaseLanding()
		{
			_isPlayerLanded = false;
			_shuttleBody = null;
		}
	}
}

using Godot;
using System;

namespace SpaceGame.Entities.Planet
{
	public partial class Planet : StaticBody2D
	{
		// ЭКСПОРТ ПАРАМЕТРОВ
		[Export] public string PlanetName { get; set; } = "Unknown";
		[Export] public string PlanetClass {get; set; } = "M-Class (Habitable)";
		[Export] public float PlanetRadius { get; set; } = 1.0f;
		[Export] public float PlanetMass { get; set; } = 1.0f;

		// Состояние захвата
		[Export] public bool isCaptured { get; private set; } = false;
		[Export] public float CaptureProgress { get; private set; } = 0.0f; // от 0 до 100
		[Export] public float CaptureSpeed { get; private set; } = 10.0f; // процент захвата в секунду

		private Area2D _gravityArea;
		private bool _isPlayerInsideZone = false;

		// READY
		public override void _Ready()
		{
			base._Ready();

			_gravityArea = GetNode<Area2D>("%GravityArea");

			// подписываемся на события входа и выхода корабля в зону орбиты планеты
			_gravityArea.BodyEntered += OnShuttleEnteredOrbit;
			_gravityArea.BodyExited += OnShuttleExitedOrbit;
		}

		// PHYSICS PROCESS
		public override void _PhysicsProcess(double delta)
		{
			base._PhysicsProcess(delta);

			// Логика постепенного захвата планеты, если игрок находиться на её орбите
			if (_isPlayerInsideZone && !isCaptured)
			{
				CaptureProgress += CaptureSpeed * (float)delta;
				GD.Print($"Захват планеты {PlanetName}: {Mathf.Round(CaptureProgress)}%");

				if (CaptureProgress >= 100f)
				{
					CaptureProgress = 100f;
					isCaptured = true;
					OnPlanetCaptured();
				}
			}
		}

		// ON SHUTTLE ENTERED ORBIT
		public void OnShuttleEnteredOrbit(Node2D body)
		{
			if (body.Name == "Shuttle")
			{
				_isPlayerInsideZone = true;
				GD.Print($"Шаттл вышел на орбиту {PlanetName}");
			}
		}

		// ON SHUTTLE EXITED ORBIT
		public void OnShuttleExitedOrbit(Node2D body)
		{
			if (body.Name == "Shuttle")
			{
				_isPlayerInsideZone = false;
				GD.Print($"Шаттл покинул орбиту планеты {PlanetName}");
			}
		}

		// ON PLANET CAPTURED
		public void OnPlanetCaptured()
		{
			GD.Print($"Планета {PlanetName} полностью захвачена игроком!");
		}


	}
}

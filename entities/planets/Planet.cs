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

		[Export] public bool isCaptured { get; private set; } = false;
		[Export] public float CaptureProgress { get; private set; } = 0.0f; 
		[Export] public float CaptureSpeed { get; private set; } = 10.0f; 

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
					_atmosphereLine.DefaultColor = _initialPlanetColor.Lerp(Colors.Green, progressFactor);
				}


				if (CaptureProgress >= 100f)
				{
					CaptureProgress = 100f;
					isCaptured = true;
					OnPlanetCaptured();
				}
				
				// Сбрасываем флаг. Если шаттл взлетит на ЛКМ — контакт разорвется
				_isPlayerLanded = false;
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
			GD.Print($"Планета {PlanetName} полностью захвачена игроком!");
			// ИСПРАВЛЕНО: Добавлен знак % для поиска по уникальному имени
			var atmosphere = GetNode<Line2D>("%AtmosphereLine");
			if (atmosphere != null)
			{
				atmosphere.DefaultColor = Colors.Green;
			}
		}

		// Этот метод автоматически вызывает сам Шаттл при физическом касании
		public void RegisterPlayerLanding(RigidBody2D shuttle)
		{
			_shuttleBody = shuttle;
			_isPlayerLanded = true;
		}
	}
}

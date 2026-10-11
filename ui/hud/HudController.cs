using Godot;
using System;
using Vector2 = Godot.Vector2;

namespace SpaceGame.UI
{
	public partial class HudController : Control
	{
		// Ссылки на текстовые блоки
		private Label _systemInfoLabel;
		private Label _flightDataLabel;

		// БЫЛО: private RigidBody2D _shuttle;
		// СТАЛО:
		private SpaceGame.Entities.Shuttle.Shuttle _shuttle;


		// READY
		public override void _Ready()
		{
			base._Ready();

			_systemInfoLabel = GetNode<Label>("%SystemInfoLabel");
			_flightDataLabel = GetNode<Label>("%FlightDataLabel");
		}

		// Метод, который мир или шаттл вызовет при старте, чтобы подключить приборы к кораблю
		// INITIALIZE
		public void Initialize(SpaceGame.Entities.Shuttle.Shuttle shuttle)
		{
			_shuttle = shuttle;
		}

		// PROCESS
		public override void _Process(double delta)
		{
			base._Process(delta);

			if (_shuttle == null) return;

			UpdateSystemInfo();
			UpdateFlightData();
		}

		// UPDATE SYSTEM INFO
		private void UpdateSystemInfo()
		{
			// Пока зашьем эти данные жестко как константы, 
			// но архитектура готова к тому, чтобы в будущем брать их из генератора планет
			string model = "SHUTTLE-MK4";
			string starName = "Kepler-452";
			string starClass = "G2V";
			string radius = "1.11 R☉";
			string mass = "1.03 M☉";
			int capturedBodies = 0;
			int totalBodies = 5;

			_systemInfoLabel.Text =
				$"MODEL: {model}\n" +
				$"SECTOR STAR: {starName}\n" +
				$"STAR CLASS: {starClass}\n" +
				$"RADIUS: {radius}\n" +
				$"MASS: {mass}\n" +
				$"BODIES: {capturedBodies}/{totalBodies}";

		}

		// UPDATE FLIGHT DATA
		private void UpdateFlightData()
		{
			Vector2 pos = _shuttle.GlobalPosition;
			float velocityMagnitude = _shuttle.LinearVelocity.Length();
			int thrustPercent = Mathf.RoundToInt(_shuttle.CurrentThrust * 100f);

			float altitude = 0.0f; 
			float gravity = 0.0f;

			float displayX = pos.X;
			float displayY = pos.Y * -1f;

			string text = 
				$"--- FLIGHT TELEMETRY ---\n" +
				$"THR: {thrustPercent}%\n" +
				$"POS: X:{Mathf.Round(displayX)} Y:{Mathf.Round(displayY)}\n" +
				$"VEL: {Mathf.Round(velocityMagnitude)} km/h\n" +
				$"ALT: {altitude:F1}\n" +
				$"GRV: {gravity:F2}g\n\n";

			if (_shuttle.IsAutopilotActive)
			{
				_flightDataLabel.SelfModulate = new Color(0.2f, 0.6f, 1.0f, 1.0f);

				string tgt = _shuttle.TargetPlanetName;
				string exe = _shuttle.AutopilotExeState;
				
				float distance = 0f;
				float captureProgress = 0f;

				if (_shuttle.TargetPlanetObj != null)
				{
					distance = _shuttle.GlobalPosition.DistanceTo(_shuttle.TargetPlanetObj.GlobalPosition);
					// Считываем прогресс захвата напрямую из скрипта текущей планеты
					captureProgress = _shuttle.TargetPlanetObj.CaptureProgress;
				}

				text += 
					$"--- AUTOPILOT ENGAGED ---\n" +
					$"TGT: {tgt}\n";

				if (exe == "SELECTING...")
				{
					text += $"EXE: {exe}\n";
				}
				else if (exe == "LANDED")
				{
					// ТЕХНИЧЕСКИЙ ВЫВОД: При посадке пишем статус захвата планеты в реальном времени!
					text += $"EXE: {exe} | CAPTURING: {Mathf.Round(captureProgress)}%\n";
				}
				else 
				{
					float dv = 5000f - velocityMagnitude;
					text += $"EXE: {exe} (DV:{Mathf.Round(dv)} D:{Mathf.Round(distance)} T+{_shuttle.AutopilotTimer:F1})\n";
				}
			}
			else
			{
				_flightDataLabel.SelfModulate = Colors.White; 
			}

			_flightDataLabel.Text = text;
		}




	}
}

using Godot;
using System;
using Vector2 = Godot.Vector2;

namespace SpaceGame.UI
{
	public partial class HudController : Control
	{
		// Ссылки на текстовые блоки
		private RichTextLabel _systemInfoLabel;
		private Label _flightDataLabel;

		// БЫЛО: private RigidBody2D _shuttle;
		// СТАЛО:
		private SpaceGame.Entities.Shuttle.Shuttle _shuttle;


		// READY
		public override void _Ready()
		{
			base._Ready();

			_systemInfoLabel = GetNode<RichTextLabel>("%SystemInfoLabel");
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
			// Базовые заглушки, если автопилот выключен

			string starName = "UNKNOWN SECTOR";
			string starClass = "???";
			string radius = "?.?? R☉";
			string mass = "?.?? M☉";
			int capturedCount = 0;
			int totalCount = 10;

			// Считаем общее количество захваченных планет в группе для счетчика BODIES
			var planets = GetTree().GetNodesInGroup("planets");
			totalCount = planets.Count;
			foreach (Node node in planets)
			{
				if (node is SpaceGame.Entities.Planet.Planet p && p.isCaptured) capturedCount++;
			}

			// Если автопилот включен и у нас есть цель — запускаем сканер дешифровки!
			if (_shuttle != null && _shuttle.IsAutopilotActive && _shuttle.TargetPlanetObj != null)
			{
				var target = _shuttle.TargetPlanetObj;
				float progress = target.CaptureProgress; // Прогресс от 0 до 100

				starName = target.PlanetName.ToUpper();
				
				// Запускаем метод дешифровки для каждого параметра на основе текущего прогресса!
				starClass = ScrambleText(target.PlanetClass.ToUpper(), progress);
				radius = ScrambleText($"{target.PlanetRadius:F2}", progress);
				mass = ScrambleText($"{target.PlanetMass:F2}", progress);
			}

			_systemInfoLabel.Text = 
				$"SECTOR STAR: {starName}\n" +
				$"STAR CLASS: {starClass}\n" +
				$"RADIUS: {radius}\n" +
				$"MASS: {mass}\n" +
				$"BODIES: {capturedCount}/{totalCount}";
		}

		// МЕТОД КИНЕМАТОГРАФИЧНОЙ ДЕШИФРОВКИ ТЕКСТА
		private string ScrambleText(string originalText, float progress)
		{
			// Если прогресс дошел до 50%, принудительно оборачиваем ВЕСЬ текст в белый цвет
			if (progress >= 50f) return $"[color=white]{originalText}[/color]";

			var rand = new Random();
			string glitchChars = "!@#$%^&*()_+-=[]{}|;':\",./<>?";
			
			// Используем System.Text.StringBuilder для высокопроизводительной сборки строки из тегов
			var bbcodeBuilder = new System.Text.StringBuilder();

			float clearThreshold = progress / 50f;

			for (int i = 0; i < originalText.Length; i++)
			{
				char currentChar = originalText[i];

				// Пропускаем спецсимволы и пробелы без покраски, чтобы не ломать верстку HUD
				if (currentChar == ' ' || currentChar == '(' || currentChar == ')' || currentChar == '☉')
				{
					bbcodeBuilder.Append(currentChar);
					continue;
				}

				// Вычисляем хаотичный вес для каждого конкретного символа
				float charWeight = (float)i / originalText.Length;

				// Если символ прошел проверку прогресса — он расшифрован! Оборачиваем его в БЕЛЫЙ цвет
				if ((float)rand.NextDouble() <= clearThreshold)
				{
					bbcodeBuilder.Append($"[color=white]{currentChar}[/color]");
				}
				else
				{
					// Если символ еще зашифрован — берем случайную глиф-букву и красим в СЕРЫЙ цвет
					char glitchChar = glitchChars[rand.Next(glitchChars.Length)];
					bbcodeBuilder.Append($"[color=gray]{glitchChar}[/color]");
				}
			}

			return bbcodeBuilder.ToString();
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

			string model = "SHUTTLE-MK4";

			string text = 
				$"--- FLIGHT TELEMETRY ---\n" +
				$"MODEL: {model}\n" +
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

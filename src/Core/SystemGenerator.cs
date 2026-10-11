using Godot;
using System;

namespace SpaceGame.Core
{
	public partial class SystemGenerator
	{
		private readonly Node2D _parentWorld;
		private readonly PackedScene _planetTemplate;

		private int _totalPlanetsToSpawn = 10;
		private float _spawnRadiusMin = 3000f;
		private float _spawnRadiusMax = 15000f;

		public SystemGenerator(Node2D parentWorld, PackedScene planetTemplate)
		{
			_parentWorld = parentWorld;
			_planetTemplate = planetTemplate;
		}

		public void GenerateSystemPlanets()
		{
			if (_parentWorld == null) return;

			if (_planetTemplate == null)
			{
				GD.PrintErr("SYSTEM_GENERATOR_ERROR: PlanetTemplate is null! Check World Inspector.");
				return;
			}

			var rand = new Random();
			GD.Print($"--- STARTING PROCEDURAL GENERATION: {_totalPlanetsToSpawn} PLANETS ---");

			// ИСПРАВЛЕНО: Заменили <- на правильное условие <
			for (int i = 1; i < _totalPlanetsToSpawn; i++)
			{
				// 1. Клонируем шаблон планеты
				var planetInstance = _planetTemplate.Instantiate<SpaceGame.Entities.Planet.Planet>();

				// ХИТРЫЙ ФИКС: Жестко зашиваем в память ложь на старте, чтобы флаги не сбоили
				// Убедись, что в твоем Planet.cs это свойство написано как IsCaptured с большой буквы!
				if (planetInstance != null)
				{
					planetInstance.isCaptured = false; // Или IsCaptured в зависимости от твоего Planet.cs
				}

				// 2. Считаем случайные круговые координаты
				float angle = (float)(rand.NextDouble() * Mathf.Tau);
				float distance = (float)(rand.NextDouble()) * (_spawnRadiusMax - _spawnRadiusMin) + _spawnRadiusMin;
				Vector2 spawnPosition = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance); // ИСПРАВЛЕНО: Умножение на дистанцию должно быть и для X, и для Y!

				planetInstance.GlobalPosition = spawnPosition;

				// 3. Задаём уникальные Sci-fi параметры
				planetInstance.PlanetName = $"Sector-Star-PX-{i}";

				_parentWorld.AddChild(planetInstance);

				GD.Print($"Planet {planetInstance.PlanetName} spawned at X:{Mathf.Round(spawnPosition.X)} Y:{Mathf.Round(spawnPosition.Y)}");
			}
			GD.Print("--- PROCEDURAL GENERATION COMPLETED ---");
		}


	}
}

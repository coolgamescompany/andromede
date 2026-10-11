using Godot;
using System;
using System.Collections.Generic;

namespace SpaceGame.Core
{
	public partial class SystemGenerator
	{
		private readonly Node2D _parentWorld;
		private readonly PackedScene _planetTemplate;

		private int _totalPlanetsToSpawn = 10;
		private float _spawnRadiusMin = 15000f;
		private float _spawnRadiusMax = 100000f;

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
				GD.PrintErr("SYSTEM_GENERATOR_ERROR: PlanetTemplate is null!");
				return;
			}

			var rand = new Random();
			GD.Print($"--- STARTING PROCEDURAL GENERATION: {_totalPlanetsToSpawn} PLANETS ---");

			// Создаем временный список для хранения позиций уже созданных планет
			List<Vector2> spawnedPositions = new List<Vector2>();
			float minDistanceBetweenPlanets = 3500f; // Безопасное расстояние в пикселях между планетами

			for (int i = 1; i <= _totalPlanetsToSpawn; i++)
			{
				var planetInstance = _planetTemplate.Instantiate<SpaceGame.Entities.Planet.Planet>();
				if (planetInstance != null)
				{
					planetInstance.isCaptured = false;
				}

				Vector2 spawnPosition = Vector2.Zero;
				bool isValidPosition = false;
				int attempts = 0; // Предохранитель от бесконечного цикла

				// ЦИКЛ ПОДБОРА КООРДИНАТ: Крутимся, пока не найдем чистое место
				while (!isValidPosition && attempts < 100)
				{
					attempts++;
					float angle = (float)(rand.NextDouble() * Mathf.Tau);
					float distance = (float)(rand.NextDouble()) * (_spawnRadiusMax - _spawnRadiusMin) + _spawnRadiusMin;
					spawnPosition = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

					isValidPosition = true; // Считаем позицию хорошей, пока не доказано обратное

					// Проверяем расстояние до КАЖДОЙ уже созданной планеты
					foreach (Vector2 existingPos in spawnedPositions)
					{
						if (spawnPosition.DistanceTo(existingPos) < minDistanceBetweenPlanets)
						{
							isValidPosition = false; // Слишком близко! Заходим на новый круг подбора
							break;
						}
					}
				}

				// Запоминаем утвержденную позицию
				spawnedPositions.Add(spawnPosition);

				planetInstance.GlobalPosition = spawnPosition;
				planetInstance.PlanetName = $"Sector-Star-PX-{i}";
				planetInstance.ZIndex = -10;
				planetInstance.AddToGroup("planets");

				_parentWorld.AddChild(planetInstance);

				GD.Print($"Planet {planetInstance.PlanetName} spawned cleanly at X:{Mathf.Round(spawnPosition.X)} Y:{Mathf.Round(spawnPosition.Y)} after {attempts} attempts.");
			}
			GD.Print("--- PROCEDURAL GENERATION COMPLETED ---");
		}



	}
}

using System.Collections.Generic;
using ParkingCat.Config;
using ParkingCat.ValueObjects.Level;


namespace ParkingCat.Entities
{
    public sealed class Stage
    {
        public int StageId { get; }
        public int Variant { get; }

        public int Width  { get; }
        public int Height { get; }

        /// <summary>row-major (y*w+x), 1=road, 0=wall</summary>
        public int[] Road01 { get; }
        
        public LevelDifficulty Difficulty { get; }

        readonly List<Car> _cars = new();
        public IReadOnlyList<Car> Cars => _cars;


        public Stage(int stageId, int variant, int width, int height, int[] road01, LevelDifficulty difficulty)
        {
            StageId = stageId;
            Variant = variant;
            Width = width;
            Height = height;
            Road01 = road01;
            Difficulty = difficulty;
        }

        public void AddCar(Car car) => _cars.Add(car);
        public Car GetCar(int carId)
        {
            for (int i = 0; i < Cars.Count; i++)
                if (Cars[i].Id == carId) return Cars[i];
            return null;
        }

        public int ActiveCarCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _cars.Count; i++)
                    if (!_cars[i].IsLeaving) count++;
                return count;
            }
        }

        readonly List<StageTrafficLight> _trafficLights = new();
        public IReadOnlyList<StageTrafficLight> TrafficLights => _trafficLights;

        public void AddTrafficLight(StageTrafficLight t) => _trafficLights.Add(t);


        readonly List<StageConfig.OneWayLaneConfig> _oneWayLanes = new();
        public IReadOnlyList<StageConfig.OneWayLaneConfig> OneWayLanes => _oneWayLanes;

        public void AddOneWayLane(StageConfig.OneWayLaneConfig c)
            => _oneWayLanes.Add(c);
    }
}
using Plugin.Geolocator.Abstractions;

namespace PetriMap.Model
{
    public class PointOfInterest
    {
        private Position _position;
        private string _description;
        private string _type;

        public Position Position { get => _position; set => _position = value; }
        public string Description { get => _description; set => _description = value; }
        public string Type { get => _type; set => _type = value; }
    }
}

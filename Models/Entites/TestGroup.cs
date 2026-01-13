using static System.Net.Mime.MediaTypeNames;

namespace IPTS.Models.Entites
{
    public class TestGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<Test> Tests { get; set; }
    }
}

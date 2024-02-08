
namespace RadzenTest.Services
{
    public class ColorService : ServiceCollection
    {
        public string GetColor(int number)
        {
            return number == 1 ? "red" : "green";
        }
    }
}


namespace RadzenTest.Services
{
    public class ColorService
    {
        public string col { get; set; } = "";
        public string GetColor(int number)
        {
            col = number == 1 ? "red" : "green";
            return col;
        }
    }
}


namespace RadzenTest.Services
{
    public class ColorService
    {
        public event Action OnChange;
        private void NotifyDataChanged() => OnChange?.Invoke();


        private string _col = "green";
        public string col
        {
            get
            {
                return _col;
            }
            set
            {

                _col = value;
                NotifyDataChanged();
            }
        }
        public string GetColor(int number)
        {
            col = number == 1 ? "red" : "green";
            return col;
        }
    }
}

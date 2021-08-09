namespace DgtEbDllWrapper
{
    public interface IDgtEbDllFacade
    {
        void DisplayMessage(string message, int time);
        void DisplayMessageSeries(params string[] messages);
        string GetRabbitVersionString();
        void HideCongigDialog();
        void Init();
        void SetClock(string whiteClock, string blackClock, int runwho);
        void ShowCongigDialog();
    }
}
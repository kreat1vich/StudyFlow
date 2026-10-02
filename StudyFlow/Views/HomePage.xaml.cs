using System.Collections.ObjectModel;
using System.Text.Json;
using StudyFlow.Models;

namespace StudyFlow.Views;

public partial class HomePage : ContentPage
{
    public ObservableCollection<StatItem> StatsList { get; set; } = new();

    public HomePage()
    {
        InitializeComponent();
        StatsCollection.ItemsSource = StatsList;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadStatsUI(); // Оновлюємо статистику щоразу, коли заходимо на головну
    }

    private void LoadStatsUI()
    {
        StatsList.Clear();
        foreach (var pair in FocusPage.subjectStats)
        {
            StatsList.Add(new StatItem
            {
                Subject = pair.Key,
                TimeFormatted = FormatMinutes(pair.Value)
            });
        }
    }

    private string FormatMinutes(int totalMinutes)
    {
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        if (hours > 0 && minutes > 0) return $"{hours} год {minutes} хв";
        if (hours > 0) return $"{hours} год";
        return $"{minutes} хв";
    }
}
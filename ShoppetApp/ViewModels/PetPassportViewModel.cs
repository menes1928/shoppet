using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ShoppetApp.Messages;
using ShoppetApp.Models;
using ShoppetApp.Services;
using System.Collections.ObjectModel;

namespace ShoppetApp.ViewModels;

public partial class PetPassportViewModel : ObservableObject, IQueryAttributable, IRecipient<DataChangedMessage>
{
    private readonly DatabaseService _db;

    [ObservableProperty] private Pet? _pet;
    [ObservableProperty] private ObservableCollection<HealthLog> _healthLogs = [];
    [ObservableProperty] private ObservableCollection<FoodLog> _foodLogs = [];
    [ObservableProperty] private bool _isBusy;

    public int PetId { get; private set; }

    public PetPassportViewModel(DatabaseService db)
    {
        _db = db;
        WeakReferenceMessenger.Default.Register(this);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("petId", out var value))
            PetId = Convert.ToInt32(value);
    }

    public void Receive(DataChangedMessage message) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    public async Task LoadAsync()
    {
        if (PetId <= 0 || IsBusy)
            return;

        IsBusy = true;
        try
        {
            Pet = await _db.GetPetAsync(PetId);
            var logs = await _db.GetHealthLogsAsync(PetId);
            HealthLogs = new ObservableCollection<HealthLog>(logs);
            var foodLogs = await _db.GetFoodLogsAsync(PetId);
            FoodLogs = new ObservableCollection<FoodLog>(foodLogs);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync() =>
        await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task EditPetAsync()
    {
        if (Pet is null)
            return;
        await Shell.Current.GoToAsync($"petform?petId={Pet.Id}");
    }

    [RelayCommand]
    private async Task AddLogAsync()
    {
        if (Pet is null)
            return;
        await Shell.Current.GoToAsync($"healthlogform?petId={Pet.Id}");
    }

    [RelayCommand]
    private async Task EditLogAsync(HealthLog log) =>
        await Shell.Current.GoToAsync($"healthlogform?petId={PetId}&logId={log.Id}");

    [RelayCommand]
    private async Task MarkDoneAsync(HealthLog log)
    {
        if (log == null || log.Completed) return;

        if (log.IsVaccine)
        {
            if (DateTime.TryParse(log.DueDate, out var currentDue))
            {
                if (log.ValidityUnit == "Years")
                    log.DueDate = currentDue.AddYears(log.ValidityInterval).ToString("yyyy/MM/dd, HH:mm:ss");
                else if (log.ValidityUnit == "Weeks")
                    log.DueDate = currentDue.AddDays(log.ValidityInterval * 7).ToString("yyyy/MM/dd, HH:mm:ss");
                else // Months default
                    log.DueDate = currentDue.AddMonths(log.ValidityInterval).ToString("yyyy/MM/dd, HH:mm:ss");
            }
        }
        else if (log.IsMedication)
        {
            if (DateTime.TryParse(log.DueDate, out var currentDue))
            {
                log.DueDate = currentDue.AddHours(log.MedicationIntervalHours).ToString("yyyy/MM/dd, HH:mm:ss");
            }
            
            if (log.DosageRemaining > 0)
                log.DosageRemaining--;
                
            if (log.DosageRemaining <= 0)
                log.Completed = true;
        }
        else // Checkup
        {
            log.Completed = true;
        }

        await _db.SaveHealthLogAsync(log);
        WeakReferenceMessenger.Default.Send(DataChangedMessage.Instance);
    }
    
    [RelayCommand]
    private async Task MarkCompletedAsync(HealthLog log)
    {
        if (log == null) return;
        log.Completed = true;
        await _db.SaveHealthLogAsync(log);
        WeakReferenceMessenger.Default.Send(DataChangedMessage.Instance);
    }
    [RelayCommand]
    private async Task AddFoodLogAsync()
    {
        if (Pet is null)
            return;
        await Shell.Current.GoToAsync($"foodlogform?petId={Pet.Id}");
    }

    [RelayCommand]
    private async Task EditFoodLogAsync(FoodLog log) =>
        await Shell.Current.GoToAsync($"foodlogform?petId={PetId}&logId={log.Id}");

    [RelayCommand]
    private async Task MarkFedDoneAsync(FoodLog log)
    {
        if (log is null) return;
        await _db.MarkFoodDoneAsync(log);
        // Reload so the card refreshes (LastFed label + next feeding calc)
        WeakReferenceMessenger.Default.Send(DataChangedMessage.Instance);
    }
}


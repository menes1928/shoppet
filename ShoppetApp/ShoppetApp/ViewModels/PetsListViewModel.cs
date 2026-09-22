using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ShoppetApp.Messages;
using ShoppetApp.Models;
using ShoppetApp.Services;
using System.Collections.ObjectModel;

namespace ShoppetApp.ViewModels;

public partial class PetsListViewModel : ObservableObject, IRecipient<DataChangedMessage>
{
    private readonly ApiService _api;

    [ObservableProperty] private ObservableCollection<Pet> _pets = [];
    [ObservableProperty] private bool _isBusy;

    public PetsListViewModel(ApiService api)
    {
        _api = api;
        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(DataChangedMessage message) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    public async Task LoadAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            var pets = await _api.GetPetsAsync();
            Pets = new ObservableCollection<Pet>(pets);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddPetAsync() =>
        await Shell.Current.GoToAsync("petform");

    [RelayCommand]
    private async Task EditPetAsync(Pet pet) =>
        await Shell.Current.GoToAsync($"petform?petId={pet.Id}");

    [RelayCommand]
    private async Task OpenPetAsync(Pet pet) =>
        await Shell.Current.GoToAsync($"petpassport?petId={pet.Id}");

    [RelayCommand]
    private async Task DeletePetAsync(Pet pet)
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Delete Pet",
            $"Remove {pet.Name} and all health records?",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        await _api.DeletePetAsync(pet.Id);
        WeakReferenceMessenger.Default.Send(DataChangedMessage.Instance);
    }
}



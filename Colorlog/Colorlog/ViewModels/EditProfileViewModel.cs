using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Colorlog.ViewModels;


public partial class EditProfileViewModel : ObservableObject
{
    public Action<bool>? CloseRequested { get; set; }

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private DateTime? birthDate;

    public EditProfileViewModel()
        : this(string.Empty, DateTime.Today.AddYears(-24))
    {
    }

    public EditProfileViewModel(string initialName, DateTime initialBirthDate)
    {
        Name = initialName;
        var d = initialBirthDate.Date;
        if (d > DateTime.Today)
        {
            d = DateTime.Today;
        }

        BirthDate = d;
    }

    public string CalculatedAgeText
    {
        get
        {
            if (!BirthDate.HasValue)
            {
                return "생년월일을 선택해 주세요.";
            }

            var birth = BirthDate.Value.Date;
            if (birth > DateTime.Today)
            {
                return "생년월일은 오늘 이전이어야 합니다.";
            }

            var age = GetCompletedAgeYears(birth, DateTime.Today);
            return $"만 {age}세 (오늘 기준 · {DateTime.Today:yyyy.MM.dd})";
        }
    }

    partial void OnBirthDateChanged(DateTime? value) => OnPropertyChanged(nameof(CalculatedAgeText));

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            _ = MessageBox.Show(
                "사용자 이름을 입력해 주세요.",
                "프로필 수정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!BirthDate.HasValue)
        {
            _ = MessageBox.Show(
                "생년월일을 선택해 주세요.",
                "프로필 수정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var birth = BirthDate.Value.Date;
        if (birth > DateTime.Today)
        {
            _ = MessageBox.Show(
                "생년월일은 오늘 이전이어야 합니다.",
                "프로필 수정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var age = GetCompletedAgeYears(birth, DateTime.Today);
        if (age is < 5 or > 120)
        {
            _ = MessageBox.Show(
                "입력 가능한 나이 범위를 벗어났습니다. 생년월일을 다시 확인해 주세요.",
                "프로필 수정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    public static int GetCompletedAgeYears(DateTime birthDate, DateTime asOfDate)
    {
        birthDate = birthDate.Date;
        asOfDate = asOfDate.Date;
        var years = asOfDate.Year - birthDate.Year;
        if (asOfDate.Month < birthDate.Month
            || (asOfDate.Month == birthDate.Month && asOfDate.Day < birthDate.Day))
        {
            years--;
        }

        return Math.Max(0, years);
    }
}

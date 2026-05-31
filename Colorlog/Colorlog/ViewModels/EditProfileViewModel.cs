using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Colorlog.Models;
using Colorlog.Services;
using System;

namespace Colorlog.ViewModels;
public partial class EditProfileViewModel : ObservableObject
{
    public Action<bool>? CloseRequested { get; set; }

    private readonly DatabaseService _databaseService;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private DateTime? birthDate;
    [ObservableProperty] private bool isGenderNone = true;
    [ObservableProperty] private bool isGenderMale;
    [ObservableProperty] private bool isGenderFemale;

    public EditProfileViewModel()
        : this(new DatabaseService(), string.Empty, DateTime.Today.AddYears(-24))
    {
    }

    public EditProfileViewModel(DatabaseService databaseService, string initialName, DateTime initialBirthDate)
    {
        _databaseService = databaseService;
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
        // 1. 이름 유효성 검사
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            _ = MessageBox.Show(
                "사용자 이름을 입력해 주세요.",
                "프로필 수정",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // 2. 생년월일 유효성 검사
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

        // DB 저장 로직
        try
        {
            string? genderStr = null;
            if (IsGenderMale) genderStr = "남";
            else if (IsGenderFemale) genderStr = "여";
            //IsGenderNone 이면 null 상태 유지 (파이썬 스키마 null 허용 따라서)

            User newUser = new User
            {
                UserName = this.Name,
                Gender = genderStr,
                Age = $"만 {age}세"
            };

            _databaseService.InsertUser(newUser);
        }
        catch(Exception ex)
        {
            _ = MessageBox.Show(
                $"데이터베이스 저장 중 에러가 발생했습니다:\n{ex.Message}",
                "DB 에러",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return; // 에러가 나면 창을 닫지 않고 중단시킴
        }

        // DB 저장이 완벽히 끝나면 
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

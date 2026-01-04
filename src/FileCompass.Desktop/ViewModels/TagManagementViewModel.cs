using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileCompass.Core.Models;
using FileCompass.Desktop.Services;

namespace FileCompass.Desktop.ViewModels;

public partial class TagManagementViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Tag> _tags = [];

    [ObservableProperty]
    private Tag? _selectedTag;

    [ObservableProperty]
    private string _newTagName = string.Empty;

    [ObservableProperty]
    private string _newTagColour = "#808080";

    [ObservableProperty]
    private Tag? _editingTag;

    [ObservableProperty]
    private string _editTagName = string.Empty;

    [ObservableProperty]
    private string _editTagColour = "#808080";

    public Tag? NewlyCreatedTag { get; private set; }

    public async Task LoadTagsAsync()
    {
        var tags = await ServiceLocator.TagRepository.GetAllAsync();
        Tags = new ObservableCollection<Tag>(tags);
    }

    [RelayCommand]
    private async Task AddTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName))
            return;

        var tag = new Tag
        {
            Name = NewTagName.Trim(),
            Colour = NewTagColour
        };

        await ServiceLocator.TagRepository.AddAsync(tag);
        NewlyCreatedTag = tag;
        await LoadTagsAsync();

        NewTagName = string.Empty;
        NewTagColour = "#808080";
    }

    public void StartEdit(Tag tag)
    {
        EditingTag = tag;
        EditTagName = tag.Name;
        EditTagColour = tag.Colour;
    }

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        if (EditingTag is null || string.IsNullOrWhiteSpace(EditTagName))
            return;

        EditingTag.Name = EditTagName.Trim();
        EditingTag.Colour = EditTagColour;

        await ServiceLocator.TagRepository.UpdateAsync(EditingTag);
        await LoadTagsAsync();

        EditingTag = null;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        EditingTag = null;
    }

    public static async Task<int> GetTagUsageCountAsync(long tagId)
    {
        return await ServiceLocator.TagRepository.GetLocationCountAsync(tagId);
    }

    public async Task DeleteTagAsync(long tagId)
    {
        await ServiceLocator.TagRepository.DeleteAsync(tagId);
        await LoadTagsAsync();
    }
}

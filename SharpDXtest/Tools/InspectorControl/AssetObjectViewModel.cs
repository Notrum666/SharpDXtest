using System.Collections.ObjectModel;
using System.Reflection;

using Engine;
using Engine.BaseAssets.Components;

namespace Editor
{
    public class AssetObjectViewModel : ObjectViewModel
    {
        private RelayCommand saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(
            _ =>
            {
                InspectorControl.Current.Focus();
                inspectorAssetViewModel.Save();
            }
        );

        InspectorAssetViewModel inspectorAssetViewModel;
        public AssetObjectViewModel(InspectorAssetViewModel assetViewModel)
            : base(assetViewModel.TargetObject) 
        {
            inspectorAssetViewModel = assetViewModel;
        }
    }
}
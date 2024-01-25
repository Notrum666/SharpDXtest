using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Engine;
using Engine.Assets;

namespace Editor
{
    public class DropTextBlock : TextBlock
    {
        public DropTextBlock()
        {

        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            e.Handled = true;

            FieldViewModel fieldViewModel = DataContext as FieldViewModel;
            if (fieldViewModel is null)
                return;

            object dataObj = e.Data.GetData(DataFormats.Serializable);

            if (dataObj is null)
                return;
            
            ContentBrowserAssetViewModel asset = dataObj as ContentBrowserAssetViewModel;
            if (asset is not null)
            {
                if (!AssetsManager.TryGetAssetTypeByGuid(asset.AssetGuid, out Type type))
                    return;

                if (fieldViewModel.TargetType == typeof(Guid))
                {
                    Type expectedType;
                    if ((expectedType = fieldViewModel.TargetField.GetCustomAttribute<GuidExpectedTypeAttribute>()?.ExpectedType) is null)
                    {
                        Logger.Log(LogType.Warning, "No expected type found for Guid field " + fieldViewModel.DisplayName);
                        return;
                    }
                    if (!expectedType.IsAssignableFrom(type))
                        return;

                    fieldViewModel.Value = asset.AssetGuid;
                    return;
                }

                if (!type.IsAssignableTo(fieldViewModel.TargetType))
                    return;

                fieldViewModel.Value = AssetsManager.LoadAssetByGuid(asset.AssetGuid);

                return;
            }

            if (!dataObj.GetType().IsAssignableTo(fieldViewModel.TargetType))
                return;

            fieldViewModel.Value = dataObj;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            e.Handled = true;

            e.Effects = DragDropEffects.None;

            FieldViewModel fieldViewModel = DataContext as FieldViewModel;
            if (fieldViewModel is null)
                return;

            object dataObj = e.Data.GetData(DataFormats.Serializable);

            if (dataObj is null)
                return;

            ContentBrowserAssetViewModel asset = dataObj as ContentBrowserAssetViewModel;
            if (asset is not null)
            {
                if (!AssetsManager.TryGetAssetTypeByGuid(asset.AssetGuid, out Type type))
                    return;

                if (fieldViewModel.TargetType == typeof(Guid))
                {
                    Type expectedType;
                    if ((expectedType = fieldViewModel.TargetField.GetCustomAttribute<GuidExpectedTypeAttribute>()?.ExpectedType) is null)
                    {
                        Logger.Log(LogType.Warning, "No expected type found for Guid field " + fieldViewModel.DisplayName);
                        return;
                    }
                    if (!expectedType.IsAssignableFrom(type))
                        return;

                    e.Effects = DragDropEffects.Link;
                    return;
                }

                if (!type.IsAssignableTo(fieldViewModel.TargetType))
                    return;

                e.Effects = DragDropEffects.Link;

                return;
            }

            if (!dataObj.GetType().IsAssignableTo(fieldViewModel.TargetType))
                return;

            e.Effects = DragDropEffects.Link;
        }
    }
}

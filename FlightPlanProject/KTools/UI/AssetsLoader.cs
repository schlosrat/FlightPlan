using UnityEngine;
using SpaceWarp.API.Assets;

namespace FlightPlan.KTools.UI;

public class AssetsLoader
{
    public static Texture2D LoadIcon(string path)
    {
        Texture2D _imageTexture = AssetManager.GetAsset<Texture2D>($"{FlightPlanPlugin.Instance.Info.Metadata.GUID}/images/{path}.png");

        //   Check if the texture is null
        if (_imageTexture == null)
        {
            // Print an Error message to the Console
            FlightPlanPlugin.Logger.LogError("Failed to load image texture from path: " + path);

            // Print the full path of the resource
            FlightPlanPlugin.Logger.LogDebug("Full resource path: " + Application.dataPath + "/" + path);

            // Print the type of resource that was expected
            FlightPlanPlugin.Logger.LogDebug("Expected resource type: Texture2D");
        }

        return _imageTexture;
    }
}

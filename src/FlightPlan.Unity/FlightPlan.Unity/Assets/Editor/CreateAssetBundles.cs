/* Flight Plan
 * Copyright (C) 2024  schlosrat
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using UnityEditor;
using System.IO;

namespace FlightPlan.Unity.Editor
{
    /// <summary>
    /// This script adds a menu item to the Unity Editor under "Assets" called "Build AssetBundles" that will
    /// build all asset bundles in the project and copy them to the "plugin_template/assets/bundles" directory.
    /// </summary>
    public static class CreateAssetBundles
    {
        private const string AssetBundleDirectory = "Assets/AssetBundles";
        // Relative path from the Unity project directory to the target directory
        private const string TargetDirectory = "../../../plugin_template/assets/bundles";

        [MenuItem("Assets/Build AssetBundles")]
        private static void BuildAllAssetBundles()
        {
            // Ensure the AssetBundle directory exists
            if (!Directory.Exists(AssetBundleDirectory))
            {
                Directory.CreateDirectory(AssetBundleDirectory);
            }

            // Build the asset bundles
            BuildPipeline.BuildAssetBundles(
                AssetBundleDirectory,
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows
            );

            // Delete existing bundles in the target directory
            if (Directory.Exists(TargetDirectory))
            {
                var files = Directory.GetFiles(TargetDirectory, "*.bundle");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            else
            {
                Directory.CreateDirectory(TargetDirectory);
            }

            // Copy the newly built bundles to the target directory
            var newBundles = Directory.GetFiles(AssetBundleDirectory, "*.bundle");
            foreach (var bundle in newBundles)
            {
                var destFile = Path.Combine(TargetDirectory, Path.GetFileName(bundle));
                File.Copy(bundle, destFile, overwrite: true);
            }
        }
    }
}

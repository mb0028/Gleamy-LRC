using UnityEditor.Android;
using System.IO;
using System.Xml.Linq;
using UnityEngine;

public class MBManifestTagger : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        AddAccess(path, UIScript.READ_MEDIA_AUDIO);
    }
    
    private void AddAccess(string path, string androidAccess)
    {
        string manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

        if (!File.Exists(manifestPath)) 
            manifestPath = Path.Combine(path, "unityLibrary/src/main/AndroidManifest.xml");

        XDocument manifest = XDocument.Load(manifestPath);
        XElement manifestRoot = manifest.Element("manifest");
        XNamespace androidNamespace = "http://schemas.android.com/apk/res/android";

        XElement newAccess = new("uses-permission");
        newAccess.Add(new XAttribute(androidNamespace + "name", androidAccess));

        XElement applicationNode = manifestRoot.Element("application");
        if (applicationNode != null)
        {
            applicationNode.AddBeforeSelf(newAccess);
            Debug.Log($"Successfully added Access {androidAccess} to the AndroidManifest.xml");
        }
        manifest.Save(manifestPath);
    }
}
#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>The runtime art contract shared by server species and the client catalogs.</summary>
public class FishAssetTests
{
    private const string SettingsPath = "Assets/CatchFish/Settings/";

    [TestCase("f1", "tin-minnow", 48, 32)]
    [TestCase("f2", "brick-bream", 48, 32)]
    [TestCase("f3", "stripe-sprat", 48, 32)]
    [TestCase("f4", "needlefish", 80, 24)]
    [TestCase("f5", "mirror-carp", 64, 40)]
    [TestCase("f6", "lantern-jelly", 32, 48)]
    [TestCase("f7", "crownfin", 64, 48)]
    [TestCase("f8", "ribbon-eel", 96, 32)]
    [TestCase("f9", "puffer", 40, 40)]
    [TestCase("f10", "snapper", 56, 40)]
    [TestCase("f11", "armored-grouper", 64, 48)]
    [TestCase("f12", "angler", 56, 48)]
    public void RuntimeSpriteMatchesSpeciesArtContract(string id, string filename, int width, int height)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<FishSkinCatalog>(SettingsPath + "FishSkinCatalog.asset");
        Assert.That(catalog, Is.Not.Null);
        var skin = catalog.Get(id);
        Assert.That(skin.sprite, Is.Not.Null, id);
        var sprite = skin.sprite;
        var path = AssetDatabase.GetAssetPath(sprite);
        Assert.That(path, Is.EqualTo("Assets/CatchFish/Art/Fish/" + filename + ".png"));
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Assert.That(importer, Is.Not.Null);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        importer.GetSourceTextureWidthAndHeight(out var sourceWidth, out var sourceHeight);
        TestContext.WriteLine($"{id}: source={sourceWidth}x{sourceHeight}, imported={sprite.texture.width}x{sprite.texture.height}, rect={sprite.rect}, PPU={sprite.pixelsPerUnit}, mesh={settings.spriteMeshType}");
        Assert.That(skin.color, Is.EqualTo(Color.white), "Art must not be tinted");
        Assert.That(new Vector2Int(sourceWidth, sourceHeight), Is.EqualTo(new Vector2Int(width, height)), "Source PNG dimensions");
        Assert.That(new Vector2Int(sprite.texture.width, sprite.texture.height), Is.EqualTo(new Vector2Int(width, height)), "Imported texture dimensions");
        Assert.That(sprite.rect, Is.EqualTo(new Rect(0f, 0f, width, height)), "Full canvas sprite rect");
        Assert.That(sprite.pivot, Is.EqualTo(new Vector2(width * 0.5f, height * 0.5f)), "Centered pixel pivot");
        Assert.That(sprite.pixelsPerUnit, Is.EqualTo((float)height), "One world unit per canvas height");
        Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
        Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
        Assert.That(settings.spritePivot, Is.EqualTo(new Vector2(0.5f, 0.5f)));
        Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
        Assert.That(importer.mipmapEnabled, Is.False);
        Assert.That(sprite.texture.mipmapCount, Is.EqualTo(1));
        Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
        Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
        Assert.That(importer.alphaIsTransparency, Is.True);
        Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect));
    }

    [Test]
    public void ServerRosterHasExactlyOneExplicitSkinAndBehaviorMappingPerSpecies()
    {
        var configPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Server/content/game_config.json"));
        Assert.That(File.Exists(configPath), Is.True, "Run from the repository root Unity project; server roster is required.");
        var config = JsonConvert.DeserializeObject<GameConfig>(File.ReadAllText(configPath));
        var ids = config.Fish.Select(fish => fish.Id).ToArray();
        Assert.That(ids, Is.EquivalentTo(Enumerable.Range(1, 12).Select(index => "f" + index)));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length), "Duplicate server IDs");

        var skins = AssetDatabase.LoadAssetAtPath<FishSkinCatalog>(SettingsPath + "FishSkinCatalog.asset");
        var behaviors = AssetDatabase.LoadAssetAtPath<FishBehaviorCatalog>(SettingsPath + "FishBehaviorCatalog.asset");
        Assert.That(skins, Is.Not.Null);
        Assert.That(behaviors, Is.Not.Null);
        var skinEntries = new SerializedObject(skins).FindProperty("skins");
        var skinIds = new List<string>();
        for (var i = 0; i < skinEntries.arraySize; i++)
        {
            var entry = skinEntries.GetArrayElementAtIndex(i);
            skinIds.Add(entry.FindPropertyRelative("fishId").stringValue);
            Assert.That(entry.FindPropertyRelative("sprite").objectReferenceValue, Is.Not.Null);
        }

        var profiles = new SerializedObject(behaviors).FindProperty("profiles");
        var behaviorIds = new List<string>();
        for (var i = 0; i < profiles.arraySize; i++)
        {
            var profileIds = profiles.GetArrayElementAtIndex(i).FindPropertyRelative("fishIds");
            for (var j = 0; j < profileIds.arraySize; j++)
            {
                behaviorIds.Add(profileIds.GetArrayElementAtIndex(j).stringValue);
            }
        }

        // Inspect explicit entries: Get() fallbacks would conceal an omitted cruise species.
        Assert.That(skinIds, Is.EquivalentTo(ids), "Skin roster differs from server roster");
        Assert.That(behaviorIds, Is.EquivalentTo(ids), "Behavior roster differs from server roster");
    }
}
#endif

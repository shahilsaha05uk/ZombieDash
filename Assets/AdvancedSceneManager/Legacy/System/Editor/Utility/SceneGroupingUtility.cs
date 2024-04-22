using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace AdvancedSceneManager.Editor.Utility
{

    internal static class SceneGroupingUtility
    {

        public class Item
        {
            public (SceneCollection collection, bool asLoadingScreen)[] collections;
            public DynamicCollection[] dynamicCollections;
            public Scene scene;
            public bool? include;
            public override string ToString() => scene.name;
        }

        public static Item[] GetItems(Profile profile, params SceneAsset[] objects) =>
            GetItems(profile, objects.
                Select(AssetDatabase.GetAssetPath).
                Select(SceneManager.assets.allScenes.Find).
                Where(s => s).
                ToArray());

        public static Item[] GetItems(Profile profile, params Scene[] scenes) =>
            scenes?.
            Select(scene =>
            {

                if (!scene)
                    return null;

                var collections = scene.FindCollections(profile);
                var isSplashScreen = profile ? profile.splashScreen == scene : false;

                var isLoadingScreen =
                    collections.Any(c => c.asLoadingScreen) ||
                    (profile ? profile.loadingScreen == scene : false);

                var forceInclude = collections.Any() || isSplashScreen || isLoadingScreen || ((profile ?? Profile.current)?.IsSet(scene.path, includeStandalone: false) ?? false);

                return new Item()
                {
                    scene = scene,
                    collections = collections?.ToArray() ?? Array.Empty<(SceneCollection collection, bool asLoadingScreen)>(),
                    dynamicCollections = profile ? profile.dynamicCollections.Where(c => c.scenes.Contains(scene.path)).ToArray() : Array.Empty<DynamicCollection>(),
                    include = forceInclude ? null : new bool?(profile ? profile.IsSet(scene.path, includeStandalone: true) : false)
                };

            })?.
            OfType<Item>()?.ToArray() ?? Array.Empty<Item>();

        public static Dictionary<string, List<(Item item, bool isLoadingScreen)>> Group(SceneAsset[] scenes, Profile profile = null, bool showAll = false) =>
            Group(profile, GetItems(profile, scenes), showAll);

        public static Dictionary<string, List<(Item item, bool isLoadingScreen)>> Group(Scene[] scenes, Profile profile = null, bool showAll = false) =>
            Group(profile, GetItems(profile, scenes), showAll);

        public static Dictionary<string, List<(Item item, bool isLoadingScreen)>> Group(Profile profile, Item[] items, bool showAll = false)
        {

            //If we don't have a profile, just show all scenes in a flat list
            if (!profile)
                return new Dictionary<string, List<(Item item, bool isLoadingScreen)>>()
                {
                    { "", items.Select(o => (o, false)).ToList() }
                };

            items = items.Where(item => showAll || !profile || profile.scenes.Any(s => s.path == item.scene.path)).ToArray();

            Item splashScreen = null;
            Item defaultLoadingScreen = null;
            Dictionary<SceneCollection, List<(Item scene, bool isLoadingScreen)>> collectionScenes = new Dictionary<SceneCollection, List<(Item scene, bool isLoadingScreen)>>();
            Dictionary<DynamicCollection, List<Item>> dynamicCollectionScenes = new Dictionary<DynamicCollection, List<Item>>();
            List<Item> standalone = new List<Item>();

            //Disassemble input list
            foreach (var item in items)
            {

                if (profile && profile.splashScreen == item.scene)
                    splashScreen = item;
                else if (profile && profile.loadingScreen == item.scene)
                    defaultLoadingScreen = item;
                else if (item.collections.Any())
                    foreach (var collection in item.collections)
                    {

                        if (!collectionScenes.ContainsKey(collection.collection))
                            collectionScenes[collection.collection] = new List<(Item scene, bool isLoadingScreen)>();

                        collectionScenes[collection.collection].Add((item, collection.asLoadingScreen));

                    }
                else if (item.dynamicCollections.Any())
                    foreach (var collection in item.dynamicCollections)
                    {

                        if (!dynamicCollectionScenes.ContainsKey(collection))
                            dynamicCollectionScenes[collection] = new List<Item>();

                        dynamicCollectionScenes[collection].Add(item);

                    }
                else
                    standalone.Add(item);

            }


            //Reassemble variables as output dictionary
            var dict = new Dictionary<string, List<(Item item, bool isLoadingScreen)>>();

            if (splashScreen != null)
                dict.Add("Splash screen", new List<(Item item, bool isLoadingScreen)>() { (splashScreen, false) });

            if (defaultLoadingScreen != null)
                dict.Add("Default loading screen", new List<(Item item, bool isLoadingScreen)>() { (defaultLoadingScreen, false) });

            foreach (var item in collectionScenes.OrderBy(c => profile ? profile.Order(c.Key) : 0))
            {

                var sortedScenes = item.Value.OrderBy(s => s.isLoadingScreen).ThenBy(s => s.scene.scene.name);

                if (!dict.ContainsKey(item.Key.name))
                    dict.Add(item.Key.name, new List<(Item item, bool isLoadingScreen)>());

                dict[item.Key.name].AddRange(sortedScenes);

            }

            if (standalone.Any())
                _ = dict.Set("Standalone", standalone.Select(s => (s, false)).ToList());

            foreach (var item in dynamicCollectionScenes)
            {

                var sortedScenes = item.Value.OrderBy(s => s.scene.name).Select(s => (s, false)).ToArray();

                var title = item.Key.title;
                if (string.IsNullOrWhiteSpace(title))
                    title = "Standalone";

                if (!dict.ContainsKey(title))
                    dict.Add(title, new List<(Item item, bool isLoadingScreen)>());

                dict[title].AddRange(sortedScenes);

            }

            return dict;

        }

    }

}

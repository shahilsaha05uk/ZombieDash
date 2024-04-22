using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace AdvancedSceneManager.Editor
{

    static class UnityFileReader
    {

        public class Obj
        {

            public string id;
            public string name;
            public string type;
            public string script;
            public string parent;
            public string father;
            public bool hasParent;

            public List<string> componentIDs = new();
            public List<Obj> components = new();

            public List<Obj> children;
            public Obj transform;

            public bool isTransform => type is "Transform" or "RectTransform";
            public bool isScript => !string.IsNullOrEmpty(script);
            public bool isGameObject => type is "GameObject";

            public Obj(string id, string name, string type, string script, string parent, string father, List<string> components)
            {
                this.id = id;
                this.name = name;
                this.script = script;
                this.type = type;
                this.parent = parent;
                this.father = father;
                componentIDs = new();
                children = new();
                this.componentIDs = components;
            }

            public string ToString(int indent)
            {

                var sb = new StringBuilder();

                sb.Append($"{new string('-', indent * 2)}{name}");

                foreach (var c in components.Where(c => !c.isTransform))
                    sb.Append($"\n{c.ToString(indent + 1)}");

                foreach (var obj in children)
                    sb.Append($"\n{obj.ToString(indent + 1)}");

                return sb.ToString();

            }

            public override string ToString() =>
                ToString(1);

        }

        public static Obj[] GetObjects(string file)
        {

            var contents = File.ReadAllText(file);

            var objs = new List<Obj>();
            var sb = new StringBuilder();

            using var reader = new StringReader(contents);
            var line = "";

            var id = "";
            var parent = "";
            var father = "";
            var type = "";
            var name = "";
            var script = "";
            var components = new List<string>();

            void CheckNewObj(string line)
            {

                if (!line.Trim().StartsWith("--- !u!"))
                    return;

                if (HasValidObj())
                    objs.Add(new(id, name, type, script, parent, father, components.ToList()));

                id = line[(line.IndexOf('&') + 1)..];
                parent = null;
                type = null;
                name = null;
                father = null;
                script = null;
                components.Clear();

            }

            void CheckTag(string line, string tag, ref string value)
            {
                if (line.Contains(tag))
                {
                    value = line[(line.IndexOf(':') + 2)..];
                    if (value.StartsWith("{fileID: "))
                        value = value.Remove(value.Length - 1).Replace("{fileID: ", "");
                }
            }

            void CheckType(string line, ref string value)
            {

                if (line.First() is ' ' or '%' or '-')
                    return;

                if (line.EndsWith(':'))
                    type = line.Remove(line.Length - 1);

            }

            void CheckComponent(string line, List<string> components)
            {

                var value = "";
                CheckTag(line, "- component:", ref value);
                if (!string.IsNullOrEmpty(value))
                    components.Add(value);

            }

            bool HasValidObj()
            {

                if (id?.Length < 2) return false;
                if (string.IsNullOrEmpty(type)) return false;

                return true;

            }

            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
            {

                CheckNewObj(line);

                CheckType(line, ref type);
                CheckComponent(line, components);
                CheckTag(line, "m_GameObject:", ref parent);
                CheckTag(line, "m_Father:", ref father);
                CheckTag(line, "m_Name:", ref name);
                CheckTag(line, "m_Script:", ref script);

            }

            GatherComponents();
            FindParents();
            FindScripts();
            FixNames();

            return objs.Where(o => !o.hasParent).ToArray();

            void GatherComponents()
            {
                foreach (var obj in objs)
                {
                    foreach (var id in obj.componentIDs)
                    {
                        var c = objs.FirstOrDefault(o => o.id == id);
                        if (c is not null)
                        {
                            obj.components.Add(c);
                            c.hasParent = true;
                            if (c.isTransform)
                                obj.transform = c;
                        }
                    }
                }
            }

            void FindParents()
            {

                foreach (var obj in objs.Where(o => o.type == "GameObject"))
                {

                    obj.transform ??= obj.components.FirstOrDefault(o => o.isTransform);
                    if (obj.transform is null)
                        continue;

                    var parent = objs.FirstOrDefault(o => o.componentIDs.Contains(obj.transform?.father));

                    if (parent is not null)
                    {
                        obj.parent = parent?.id;
                        parent.children.Add(obj);
                        obj.hasParent = true;
                    }

                }

            }

            void FindScripts()
            {

                foreach (var obj in objs.Where(o => o.isScript).ToArray())
                {

                    if (!obj.script.Contains("guid:"))
                    {
                        obj.script = null;
                        obj.name = obj.type;
                        continue;
                    }

                    var guid = obj.script[(obj.script.IndexOf("guid:") + 6)..];
                    guid = guid.Remove(guid.IndexOf(","));
                    obj.script = AssetDatabase.GUIDToAssetPath(guid);
                    obj.name = Path.GetFileNameWithoutExtension(obj.script);

                }

            }

            void FixNames()
            {
                foreach (var obj in objs.Where(o => string.IsNullOrEmpty(o.name)))
                    obj.name = obj.type;

            }

        }

        public static string GetFriendlyString(string file) =>
            string.Join("\n", GetObjects(file).Select(o => o.ToString())).TrimStart('\n');

    }

}

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace Umoya.CLI.Tasks
{
    static class Resolve
    {

        public static string ZMODHome = @"C:\Users\nva\Documents\GitHub\ZMOD\ZMOD";
        public static string UmoyaHome = @"C:\Users\nva\Documents\GitHub\ZMOD\Umoya";

        public static string UmoyaResourcesHome = @"C:\Users\nva\Documents\GitHub\ZMOD\Umoya\resources\resources";

        public static void Do()
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("Resolving Umoya resources i.e. model, data and code");
            Console.WriteLine("===================================================");

            XmlDocument doc = new XmlDocument();
            doc.Load("resources.csproj");
            XmlNodeList list = doc.DocumentElement.GetElementsByTagName("PackageReference");
            foreach(XmlNode xnode in list)
            {
                string ResourceName = xnode.Attributes["Include"].Value.ToString();
                string ResourceVersion = xnode.Attributes["Version"].Value.ToString();
                XmlDocument ResourceSpecDoc;                
                Resource.Types ResourceType = GetResourceType(ResourceName, ResourceVersion, out ResourceSpecDoc);
                FixByResourceType(ResourceName,ResourceVersion , ResourceType, ResourceSpecDoc);
                
            }    

            

        }

        private static string GetZMODResourceType(string UmoyaResourceType)
        {
            if(UmoyaResourceType.ToLower().Equals("model")) return "Models";
            else if(UmoyaResourceType.ToLower().Equals("code")) return "Code";
            else return "Data";
        }

        public static void FixByResourceType(string ResourceName, string ResourceVersion ,Resource.Types ResourceType, XmlDocument ResourceSpecDoc)
        {        
    
            //Resolve dependencies as well if present
            Dictionary<string,string> ListOfDependencies = GetListOfDependencies(ResourceSpecDoc);
            Console.WriteLine(">> Resource : " + ResourceType.ToString() + " " + ResourceName + " " + ResourceVersion + " Dependencies (" + ListOfDependencies.Count + ")");
            //Copy File from resource / version / contentFiles / resource type / resourceName to Zmod / resourcetype            
            string SourcePath = UmoyaResourcesHome +  "\\" + ResourceName +  "\\" + ResourceVersion + "\\contentFiles\\" + ResourceType; 
            string DestinationPath = ZMODHome + "\\" + GetZMODResourceType(ResourceType.ToString());

            if(ResourceType.Equals(Resource.Types.Code))
            {
                DestinationPath = DestinationPath + "\\" + ResourceName;
                Directory.CreateDirectory(DestinationPath);
            }

            Console.WriteLine("Source " + SourcePath + " Destination " + DestinationPath);
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(SourcePath, "*", 
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(SourcePath, DestinationPath));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(SourcePath, "*.*", 
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(SourcePath, DestinationPath), true);
            
            if(ListOfDependencies.Count > 0)
            {                
                foreach(string DependentResourceName in ListOfDependencies.Keys)
                {
                    string DependentResourceVersion = ListOfDependencies[DependentResourceName].ToString();
                    XmlDocument DependentResourceSpecDoc;                
                    Resource.Types DependentResourceType = GetResourceType(DependentResourceName, DependentResourceVersion, out DependentResourceSpecDoc);
                    FixByResourceType(DependentResourceName,DependentResourceVersion , DependentResourceType, DependentResourceSpecDoc);
                }
            }
        }

        public static Resource.Types GetResourceType(string ResourceName, string ResourceVersion, out XmlDocument ResourceSpecDoc)
        {
            string ResourceSpecFile = Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "resources" + System.IO.Path.DirectorySeparatorChar + ResourceName.ToLower() + System.IO.Path.DirectorySeparatorChar + ResourceVersion + System.IO.Path.DirectorySeparatorChar + "resource-spec.nuspec";
            if(!File.Exists(ResourceSpecFile)) 
            {
                ResourceSpecFile = Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "resources" + System.IO.Path.DirectorySeparatorChar + ResourceName.ToLower() + System.IO.Path.DirectorySeparatorChar + ResourceVersion + System.IO.Path.DirectorySeparatorChar + ResourceName.ToLower() + ".nuspec";
                if(!File.Exists(ResourceSpecFile)) throw new Exception("Resource Spec is not found");
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(ResourceSpecFile);
            XmlNodeList list = doc.DocumentElement.GetElementsByTagName("files");
            string contentFolder = list[0].Attributes["include"].Value.ToString();
            ResourceSpecDoc = doc;
            if(contentFolder.StartsWith("Models"))  return Resource.Types.Model;
            else if(contentFolder.StartsWith("Code")) return Resource.Types.Code;
            else return Resource.Types.Data;
        }

        public static Dictionary<string,string> GetListOfDependencies(XmlDocument ResourceSpecDoc)
        {
            Dictionary<string,string> ListOfDependencies = new Dictionary<string, string>();
            XmlNodeList XNodeList = ResourceSpecDoc.DocumentElement.GetElementsByTagName("dependency");
            if(XNodeList.Count > 0)
            {
                for(int i=0; i< XNodeList.Count; i++)
                {
                    XmlNode XNode = XNodeList[i];
                    string ResourceName = XNode.Attributes["id"].Value.ToString();
                    string ResourceVersion = XNode.Attributes["version"].Value.ToString();
                    ListOfDependencies.Add(ResourceName, ResourceVersion);
                }   
            }
            return ListOfDependencies;
        }
    }
}
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace STak.WinTak
{
    public class JsonMeshModelBuilder : IMeshModelBuilder 
    {
        protected string m_fileName;


        public JsonMeshModelBuilder(string fileName)
        {
            m_fileName = App.GetModelPathName(fileName);
        }


        public virtual IMeshModel Build()
        {
            var jsonText  = File.ReadAllText(m_fileName);
            var options   = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling         = JsonCommentHandling.Skip
            };
            var jsonModel = JsonSerializer.Deserialize<JsonMeshModel>(jsonText, options);

            return new MeshModel(ConvertJsonModelToRawModel(jsonModel));
        }


        private RawMeshModel ConvertJsonModelToRawModel(JsonMeshModel jsonModel)
        {
            var rawModel = new RawMeshModel
            {
                Constants  = jsonModel.Constants,
                Vertices   = TranslateConstants(jsonModel),
                Triangles  = jsonModel.Triangles,
                TextureMap = jsonModel.TextureMap
            };

            return rawModel;
        }


        private List<double[]> TranslateConstants(JsonMeshModel jsonModel)
        {
            var vertices = new List<double[]>();

            double ConvertStringToDouble(string str)
            {
                bool negate = str[0] == '-';
                int multiplier = 1;

                if (negate)
                {
                    str = str.Substring(1);
                    multiplier = -1;
                }

                if (! jsonModel.Constants.ContainsKey(str))
                {
                    throw new Exception($"Undefined constant used in Json mesh definition: {str}.");
                }

                return jsonModel.Constants[str] * multiplier;
            }

            foreach (var vertex in jsonModel.Vertices)
            {
                var xyz = new double[3];

                for (int i = 0; i < 3; ++i)
                {
                    xyz[i] = vertex[i] switch
                    {
                        double      d => d,
                        string      s => jsonModel.Constants[s],
                        JsonElement e => e.ValueKind == JsonValueKind.Number ? e.GetDouble()
                                                      : ConvertStringToDouble(e.GetString()),
                        _             => throw new Exception($"Unexpected type: {vertex[0].GetType()}.")
                    };
                }

                vertices.Add(xyz);
            }

            return vertices;
        }


        private class JsonMeshModel
        {
            public Dictionary<string, double> Constants  { get; set; }
            public List<object[]>             Vertices   { get; set; }
            public List<int[]>                Triangles  { get; set; }
            public List<double[]>             TextureMap { get; set; }
        }
    }
}


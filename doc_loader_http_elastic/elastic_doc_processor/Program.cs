﻿using System;
using Nest;
using System.Collections.Generic;
using Google.Cloud.Language.V1;


namespace elastic_doc_processor
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri ElasticUri = new Uri("http://104.197.115.64:9200");
            ConnectionSettings ElasticConnectionSettings = new ConnectionSettings(ElasticUri)
               .BasicAuthentication("elastic", "elpass")
               .DisableAutomaticProxyDetection()
               .DisableDirectStreaming()
               .PrettyJson()
               //.DefaultIndex("scp_source_pages")
               .RequestTimeout(TimeSpan.FromMinutes(2));
            ElasticClient ElasticClient = new ElasticClient(ElasticConnectionSettings);
            LanguageServiceClient client = LanguageServiceClient.Create();




            var docCount = ElasticClient.Search<PlainDocument>(s => s.Index("scp_source_pages")).Total;
            for (int i=0; i < docCount; i++)
            {
                IReadOnlyCollection<PlainDocument> Documents = ElasticClient.Search<PlainDocument>(s => s.Index("scp_source_pages")
                    .MatchAll()
                    .Skip(i)
                    .Size(1)
                    ).Documents;
                foreach (PlainDocument document in Documents)
                {
                    ScpSpecialContainmentDocument ParsedDocument = new ScpSpecialContainmentDocument();
                    //Parser part
                    ParsedDocument.PageTitle = document.Title;
                    ParsedDocument.ItemNumber = document.Title.Replace(" - SCP Foundation", "");
                    ParsedDocument.id = ParsedDocument.ItemNumber.Replace("scp-", "");
                    ParsedDocument.ObjectClass = DocumentHelpers.GetDocumentPart(document.PageSource, DocumentHelpers.ScpObjectClassSearchPatterns);
                    ParsedDocument.Body = DocumentHelpers.GetDocumentPart(document.PageSource, DocumentHelpers.ScpObjectBodyPatterns);
                    //Languagae analyzer part
                    var features = new AnnotateTextRequest.Types.Features();
                    features.ExtractDocumentSentiment = true;
                    features.ExtractEntities = true;
                    features.ExtractEntitySentiment = true;
                    AnnotateTextResponse response = client.AnnotateText(Document.FromPlainText(ParsedDocument.Body),features);
                    ParsedDocument.languageAnalyzerResult.DocumentSentiment = response.DocumentSentiment;


                    ElasticClient.Index(ParsedDocument, ind => ind
                    .Index("scp_items"));
                    Console.WriteLine("{0} from {1}, document title {2}", i, docCount, ParsedDocument.PageTitle);


                }             

            }

        }
    }
}

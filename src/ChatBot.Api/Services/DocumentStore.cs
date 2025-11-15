using ChatBot.Api.Models;
using Microsoft.Data.Sqlite;

namespace ChatBot.Api.Services;

public sealed class DocumentStore
{
    private const string DbFile = "DocumentStore.db";

    static DocumentStore()
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Documents (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                PageUrl TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public List<Documents> GetDocuments(IEnumerable<string> ids)
    {
        var idsList = ids?.Distinct().ToList() ?? [] ;
        if (idsList.Count == 0) return [];
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        var paramNames = new List<string>(idsList.Count);
        for (int i = 0; i < idsList.Count; i++)
        {
            var p = $"$p{i}";
            paramNames.Add(p);
            cmd.Parameters.AddWithValue(p, idsList[i]);
        }

        var orderByCase = "CASE Id " + string.Join(" ", idsList.Select((id, i) => $"WHEN $p{i} THEN {i}")) + " END;";

        cmd.CommandText =
            $@" SELECT Id, Title, Content, PageUrl From Documents Where Id IN ({string.Join(", ", paramNames)}) Order By {orderByCase}";
        var results = new List<Documents>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new Documents(
                Id: reader.GetString(0),
                Title: reader.GetString(1),
                Content: reader.GetString(2),
                PageUrl: reader.GetString(3)
            ));
        }
        return results;
    }

    public void SaveDocument(Documents document)
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"Insert Or Replace Into Documents (Id, Title, Content, PageUrl)  Values (@Id, @Title, @Content, @PageUrl);";
        cmd.Parameters.AddWithValue("@Id", document.Id);
        cmd.Parameters.AddWithValue("@Title", document.Title);
        cmd.Parameters.AddWithValue("@Content", document.Content);
        cmd.Parameters.AddWithValue("@PageUrl", document.PageUrl);
        cmd.ExecuteNonQuery();
    }
}
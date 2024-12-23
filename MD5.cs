using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class HexFileHasher
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Пожалуйста, передайте путь к файлу.");
            return;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Файл '{filePath}' не существует.");
            return;
        }

        try
        {
            AddHashToFile(filePath);
            Console.WriteLine($"Хеш MD5 в формате Hex был успешно добавлен в конец файла '{filePath}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }

        Console.WriteLine("Нажмите любую клавишу для завершения.");
    }

    private static string ComputeMD5(byte[] input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(input);
            StringBuilder hashString = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                hashString.Append(b.ToString("x2").ToLower()); 
            }
            return hashString.ToString();
        }
    }

    private static void AddHashToFile(string filePath)
    {
        byte[] fileBytes = File.ReadAllBytes(filePath);

        string fileHash = ComputeMD5(fileBytes);

        byte[] hashBytes = Encoding.ASCII.GetBytes(fileHash);

        using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
        {
            fs.Write(hashBytes, 0, hashBytes.Length);
        }
    }
}

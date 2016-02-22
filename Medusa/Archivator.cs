using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;

namespace Medusa
{
	public static class Archivator
	{
		public static void Archive(string archive, string password, string directory)
		{
			File.Delete(archive);
			using(Stream stream = File.OpenWrite(archive))
			{
				using(CryptoStream cryptoStream = Cipher.Encrypt(stream, password))
				using(BZip2OutputStream bz2 = new BZip2OutputStream(cryptoStream))
				using(TarArchive tar = TarArchive.CreateOutputTarArchive(bz2))
				{
					tar.RootPath = directory.Replace('\\', '/').TrimEnd('/');
					tar.AddDirectoryFiles(directory, true);
				}
			}
		}

		private static void InitCryptoProvider(string password, DESCryptoServiceProvider crypto)
		{
			var key = Encoding.Default.GetBytes(password);
			crypto.KeySize = key.Length << 3;
			crypto.IV =
			crypto.Key = key;
		}

		private static void AddDirectoryFiles(this TarArchive tarArchive, string sourceDirectory, bool recurse)
		{
			// Optionally, write an entry for the directory itself.
			// Specify false for recursion here if we will add the directory's files individually.
			//
			TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
			tarArchive.WriteEntry(tarEntry, false);

			// Write each file to the tar.
			//
			string[] filenames = Directory.GetFiles(sourceDirectory);
			foreach(string filename in filenames)
			{
				tarEntry = TarEntry.CreateEntryFromFile(filename);
				tarArchive.WriteEntry(tarEntry, true);
			}

			if(recurse)
			{
				string[] directories = Directory.GetDirectories(sourceDirectory);
				foreach(string directory in directories)
				{
					tarArchive.AddDirectoryFiles(directory, recurse);
				}
			}
		}
		public static void Restore(string archive, string password, string directory, string pattern = "*")
		{
			using(Stream stream = File.OpenRead(archive))
			{
				using(CryptoStream cryptoStream = Cipher.Decrypt(stream, password))
				using(BZip2InputStream bzip2 = new BZip2InputStream(cryptoStream))
				using(TarArchive tar = TarArchive.CreateInputTarArchive(bzip2))
				{
					tar.ExtractContents(directory);
				}
			}
		}
	}
}

using Microsoft.Win32;

namespace VSPackageManager
{
	public static class RegistryUtilities
	{
		public static bool RenameSubKey(RegistryKey parentKey, string subKeyName, string newSubKeyName)
		{
			CopyKey(parentKey, subKeyName, newSubKeyName);
			parentKey.DeleteSubKeyTree(subKeyName);
			return true;
		}
		public static bool CopyKey(RegistryKey parentKey, string keyNameToCopy, string newKeyName)
		{
			using (var destinationKey = parentKey.CreateSubKey(newKeyName))
			{
				using (var sourceKey = parentKey.OpenSubKey(keyNameToCopy))
				{
					RecurseCopyKey(sourceKey, destinationKey);
				}
			}
			return true;
		}

		private static void RecurseCopyKey(RegistryKey sourceKey, RegistryKey destinationKey)
		{
			foreach (string valueName in sourceKey.GetValueNames())
			{
				object objValue = sourceKey.GetValue(valueName);
				RegistryValueKind valKind = sourceKey.GetValueKind(valueName);
				destinationKey.SetValue(valueName, objValue, valKind);
			}

			foreach (string sourceSubKeyName in sourceKey.GetSubKeyNames())
			{
				using (var sourceSubKey = sourceKey.OpenSubKey(sourceSubKeyName))
				{
					using (var destSubKey = destinationKey.CreateSubKey(sourceSubKeyName))
					{
						RecurseCopyKey(sourceSubKey, destSubKey);
					}
				}
			}
		}
	}
}
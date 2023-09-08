using System.Text.RegularExpressions;

namespace PackageWizard.Editor
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Literally just a copy of Unity's own validation class: <see cref="UnityEditor.PackageManager.PackageValidation"/>
    /// </remarks>
    public static class PackageValidation
    {
        private static readonly Regex s_CompleteNameRegEx = new Regex("^([a-z\\d][a-z\\d-._]{0,213})$");
        private static readonly Regex s_NameRegEx = new Regex("^([a-z\\d][a-z\\d\\-\\._]{0,112})$");
        private static readonly Regex s_OrganizationNameRegEx = new Regex("^([a-z\\d][a-z\\d\\-_]{0,99})$");
        private static readonly Regex s_AllowedSemverRegEx = new Regex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$");
        private static readonly Regex s_UnityMajorVersionRegEx = new Regex("^([1-9][0-9]{3})$");
        private static readonly Regex s_UnityMinorVersionRegEx = new Regex("^([1-9])$");
        private static readonly Regex s_UnityReleaseVersionRegEx = new Regex("^(0|[1-9]\\d*)([abfp])(0|[1-9]\\d*)$");

        public static bool ValidateCompleteName(string completeName) => !string.IsNullOrEmpty(completeName) && PackageValidation.s_CompleteNameRegEx.IsMatch(completeName);

        public static bool ValidateName(string name) => !string.IsNullOrEmpty(name) && PackageValidation.s_NameRegEx.IsMatch(name);

        public static bool ValidateOrganizationName(string organizationName) => !string.IsNullOrEmpty(organizationName) && PackageValidation.s_OrganizationNameRegEx.IsMatch(organizationName);

        public static bool ValidateVersion(string version) => PackageValidation.ValidateVersion(version, out string _, out string _, out string _);

        public static bool ValidateVersion(
            string version,
            out string major,
            out string minor,
            out string patch)
        {
            major = string.Empty;
            minor = string.Empty;
            patch = string.Empty;
            Match match = PackageValidation.s_AllowedSemverRegEx.Match(version);
            if (!match.Success)
                return false;
            major = match.Groups[nameof (major)].Value;
            minor = match.Groups[nameof (minor)].Value;
            patch = match.Groups[nameof (patch)].Value;
            return true;
        }

        public static bool ValidateUnityVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return false;
            string[] strArray = version.Split('.');
            switch (strArray.Length)
            {
                case 2:
                    return PackageValidation.ValidateUnityVersion(strArray[0], strArray[1]);
                case 3:
                    return PackageValidation.ValidateUnityVersion(strArray[0], strArray[1], strArray[2]);
                default:
                    return false;
            }
        }

        public static bool ValidateUnityVersion(
            string majorVersion,
            string minorVersion,
            string releaseVersion = null)
        {
            return !string.IsNullOrEmpty(majorVersion) && PackageValidation.s_UnityMajorVersionRegEx.IsMatch(majorVersion) && !string.IsNullOrEmpty(minorVersion) && PackageValidation.s_UnityMinorVersionRegEx.IsMatch(minorVersion) && (string.IsNullOrEmpty(releaseVersion) || PackageValidation.s_UnityReleaseVersionRegEx.IsMatch(releaseVersion));
        }
    }
}
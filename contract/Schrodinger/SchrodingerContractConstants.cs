namespace Schrodinger;

public static class SchrodingerContractConstants
{
    // token
    public const string CollectionSymbolSuffix = "0";
    public const string AncestorSymbolSuffix = "1";
    public const char Separator = '-';
    public const string TokenNameSuffix = "GEN";
    public const string AncestorNameSuffix = "GEN0";
    public const int DefaultSymbolIndexStart = 2;

    // external info
    public const string InscriptionDeployKey = "__inscription_deploy";
    public const string InscriptionAdoptKey = "__inscription_adopt";
    public const string InscriptionImageKey = "__inscription_image";
    public const string AttributesKey = "__nft_attributes";
    public const string InscriptionType = "aelf";
    public const int InscriptionAmt = 1;
    public const string DeployOp = "deploy";
    public const string AdoptOp = "adopt";
    public const string Amt = "1";

    // config
    public const int DefaultMinGen = 1;
    public const int DefaultMaxGen = 10;
    public const long DefaultImageMaxSize = 10240; // 10kb
    public const long DefaultImageMaxCount = 10;
    public const long DefaultTraitValueMaxCount = 100;
    public const long DefaultAttributeMaxLength = 80;
    public const long DefaultMaxWeight = 10000;
    public const int DefaultMaxAttributePerGen = 1;
    public const int DefaultMaxAttributeTraitTypeCount = 50;
    public const int DefaultFixedTraitTypeMaxCount = 5;

    public const string ELFSymbol = "ELF";

    // math
    public const long Hundred = 100;
    public const long Denominator = 10000;
}
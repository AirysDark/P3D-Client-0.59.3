namespace Kolben
{
    public partial class ScriptProcessor
    {
        private const string RegexNumrightdot = "^[0-9]+(E[-+][0-9]+)?$";
        private const string RegexNumleftdot = @"^[-]?\d+$";
        private const string RegexLambda = @"^([a-zA-Z][a-zA-Z0-9]*([ ]*[,][ ]*[a-zA-Z][a-zA-Z0-9]*)*|\(\))[ ]*=>.+$";
        private const string RegexFunction = @"^function[ ]*\(";
        private const string RegexCatchcondition = @"^[a-zA-Z]\w*[ ]+if[ ]+.+$";
    }
}

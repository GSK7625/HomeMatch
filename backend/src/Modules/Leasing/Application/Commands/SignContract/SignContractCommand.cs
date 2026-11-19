namespace HomeMatch.Modules.Leasing.Application.Commands.SignContract;

public sealed record SignContractCommand(
    long ContractId,
    long ActorId,
    string DocumentHash,
    string OtpCode,
    string SignatureMethod = "OTP_EMAIL");

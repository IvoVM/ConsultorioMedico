namespace ClinicaSaaS.Application;

public class BusinessRuleException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

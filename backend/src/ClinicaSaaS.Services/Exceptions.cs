namespace ClinicaSaaS.Services;

public class UnauthorizedException(string message) : Exception(message);

public class BusinessRuleException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);

// The user is signed in but this is not theirs to reach (a professional opening a patient they do not treat).
public class ForbiddenException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

// The password opens both the staff and the patient account of one document, so the person has to pick one.
public class AccountChoiceException(string message) : Exception(message);

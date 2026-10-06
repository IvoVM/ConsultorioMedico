namespace ClinicaSaaS.Application;

public class ReglaNegocioException(string message) : Exception(message);

public class NoEncontradoException(string message) : Exception(message);

public class ConflictoException(string message) : Exception(message);

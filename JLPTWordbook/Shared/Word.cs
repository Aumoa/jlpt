namespace JLPTWordbook.Shared;

public record struct Word(WordComponent[] Components, Func<string?> Localizer);

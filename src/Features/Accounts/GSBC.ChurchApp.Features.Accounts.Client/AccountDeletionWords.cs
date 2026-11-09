namespace GSBC.ChurchApp.Features.Accounts.Client;

public static class AccountDeletionWords
{
    public static IReadOnlyList<string> Lines(bool yours) => yours
        ?
        [
            "You're signed out everywhere, and your passkeys stop working for this account.",
            "Your roles, your listening history, your notifications and your private calendar link go, and you're taken off any event you're a contact for.",
            "Your name stays on what you did, such as changes you made, so staff can still see who made them. Your email can be used to make a new account.",
            "Children you look after who can't sign in themselves go too, unless another parent looks after them in the app.",
            "This can't be undone.",
        ]
        :
        [
            "They're signed out everywhere, and their passkeys stop working for this account.",
            "Their roles, listening history, notifications and private calendar link go, and they're taken off any event they're a contact for.",
            "Their name stays on what they did, such as changes they made and their activity here. Their email can be used to make a new account.",
            "Children they look after who can't sign in themselves go too, unless another parent looks after them in the app.",
            "This can't be undone.",
        ];
}

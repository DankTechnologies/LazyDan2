export interface Game {
    id: number;
    league: string;
    homeTeam: string;
    awayTeam: string;
    shortHomeTeam: string;
    shortAwayTeam: string;
    state: string;
    gameTime: Date;
    downloadSelected: boolean;
    downloadStarted: boolean;
    downloadCompleted: boolean
}
export interface Game {
    id: number;
    league: string;
    homeTeam: string;
    awayTeam: string;
    shortHomeTeam: string;
    shortAwayTeam: string;
    state: string;
    gameTime: Date;
    dvr?: Dvr;
}

export interface Dvr {
    id: number;
    gameId: number;
    started: boolean;
    completed: boolean;
    game?: Game;
}

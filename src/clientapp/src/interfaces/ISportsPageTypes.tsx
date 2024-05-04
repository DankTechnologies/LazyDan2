import { Game, Dvr as IDvr } from './IGame';

export interface TopControlsProps {
    refresh: () => void;
    advanced: boolean;
}

export interface SourceSelectorProps {
    source: string;
    handleSourceChange: (event: React.ChangeEvent<HTMLSelectElement>) => void;
}

export interface SportPageProps {
    league: string;
    advanced: boolean;
}

export interface GamesTableProps {
    games: Game[] | undefined;
    onGameSelect: (event: React.MouseEvent<HTMLAnchorElement>, game: Game) => Promise<void>;
}

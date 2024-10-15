import React from 'react';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { solid } from '@fortawesome/fontawesome-svg-core/import.macro';
import { Game } from '../interfaces/IGame';

interface DvrIconProps {
    game: Game | undefined;
}

const DvrIcon: React.FC<DvrIconProps> = ({ game }) => {
    if (!game)
        return null;

    if (game.downloadSelected) {
        return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Scheduled" />;
    }

    if (game.downloadCompleted) {
        return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Completed" color='#50c77c' />;
    }

    if (game.downloadStarted) {
        return <FontAwesomeIcon className="ml-3" icon={solid("video")} title="Recording" color='#c75050' />;
    }

    return null;
};

export default DvrIcon;

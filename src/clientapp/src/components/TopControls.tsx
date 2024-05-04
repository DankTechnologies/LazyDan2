import { TopControlsProps } from "../interfaces/ISportsPageTypes";

const TopControls: React.FC<TopControlsProps> = ({ refresh, advanced }) => (
    <div className="topControls">
        <button className="button is-primary" onClick={refresh}>Refresh Games</button>
    </div>
);

export default TopControls;
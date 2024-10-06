import SportPage from '../components/SportsPage';

const Wnba = ({ advanced }: { advanced: boolean }) => {
    return <SportPage league="wnba" advanced={advanced} />;
}

export default Wnba;
import { Outlet, Link, useMatch } from "react-router-dom";

const Layout = ({ advanced }: { advanced: boolean }) => {
    const isMlbActive = useMatch("/mlb");
    const isNbaActive = useMatch("/nba");
    const isNflActive = useMatch("/nfl");
    const isCfbActive = useMatch("/cfb");
    const isNhlActive = useMatch("/nhl");
    const isDvrActive = useMatch("/dvr");

    return (
        <>
            <nav className="navbar" role="navigation" aria-label="main navigation">
                <div className="navbar-brand">
                    <Link className={`navbar-item ${isMlbActive ? 'is-active' : ''}`} to="/mlb">MLB</Link>
                    <Link className={`navbar-item pl-5 ${isNbaActive ? 'is-active' : ''}`} to="/nba">NBA</Link>
                    <Link className={`navbar-item pl-5 ${isNhlActive ? 'is-active' : ''}`} to="/nhl">NHL</Link>
                    <Link className={`navbar-item pl-5 ${isNflActive ? 'is-active' : ''}`} to="/nfl">NFL</Link>
                    <Link className={`navbar-item pl-5 ${isCfbActive ? 'is-active' : ''}`} to="/cfb">CFB</Link>
                    <Link className={`navbar-item pl-5 ${isDvrActive ? 'is-active' : ''}`} to="/dvr">DVR</Link>
                    {/* {advanced && <Link className={`navbar-item pl-5 ${isDvrActive ? 'is-active' : ''}`} to="/dvr">DVR</Link>} */}
                </div>
            </nav>
            <Outlet />
        </>
    );
}

export default Layout;
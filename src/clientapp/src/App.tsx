import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import './App.css';
import Layout from './Layout';
import Nhl from './pages/Nhl';
import Mlb from './pages/Mlb';
import Nba from './pages/Nba';
import Nfl from './pages/Nfl';
import Dvr from "./pages/Dvr";
import Cfb from "./pages/Cfb";

function App() {
  const params = new URLSearchParams(window.location.search);
  const advanced = params.get('advanced') === 'true';

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout advanced={advanced} />}>
          <Route index element={<Navigate to="/mlb" />} />
          <Route path="mlb" element={<Mlb advanced={advanced} />} />
          <Route path="nba" element={<Nba advanced={advanced} />} />
          <Route path="nfl" element={<Nfl advanced={advanced} />} />
          <Route path="cfb" element={<Cfb advanced={advanced} />} />
          <Route path="nhl" element={<Nhl advanced={advanced} />} />
          <Route path="dvr" element={<Dvr />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { createTeam, getUserTeams, getTeam, deleteTeam, type Team, type TeamMember, type TeamRole } from '@/lib/api';
import { Users, Plus, Trash2, Shield, Edit } from 'lucide-react';

export default function TeamsPage() {
  const [teams, setTeams] = useState<Team[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null);
  const [members, setMembers] = useState<TeamMember[]>([]);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);
  const [newTeamName, setNewTeamName] = useState('');
  const [newTeamDescription, setNewTeamDescription] = useState('');
  const [loadingAction, setLoadingAction] = useState(false);

  useEffect(() => {
    loadTeams();
  }, []);

  const loadTeams = async () => {
    try {
      const data = await getUserTeams();
      setTeams(data);
    } catch (e) {
      console.error('Failed to load teams:', e);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTeam = async () => {
    if (!newTeamName.trim()) return;
    
    setLoadingAction(true);
    try {
      const team = await createTeam({
        name: newTeamName,
        description: newTeamDescription || undefined,
      });
      setTeams([...teams, team]);
      setCreateDialogOpen(false);
      setNewTeamName('');
      setNewTeamDescription('');
    } catch (e) {
      console.error('Failed to create team:', e);
    } finally {
      setLoadingAction(false);
    }
  };

  const handleViewTeam = async (team: Team) => {
    setSelectedTeam(team);
    setDetailsDialogOpen(true);
    // TODO: Load members - requires backend endpoint update
    setMembers([]);
  };

  const handleDeleteTeam = async () => {
    if (!selectedTeam) return;
    
    if (!confirm(`Удалить команду "${selectedTeam.name}"? Это действие нельзя отменить.`)) {
      return;
    }

    setLoadingAction(true);
    try {
      await deleteTeam(selectedTeam.id);
      setTeams(teams.filter(t => t.id !== selectedTeam.id));
      setDetailsDialogOpen(false);
      setSelectedTeam(null);
    } catch (e) {
      console.error('Failed to delete team:', e);
    } finally {
      setLoadingAction(false);
    }
  };

  const getRoleBadgeVariant = (role: TeamRole) => {
    switch (role) {
      case 'Owner':
        return 'default';
      case 'Admin':
        return 'secondary';
      case 'Member':
        return 'outline';
      case 'Viewer':
        return 'secondary';
      default:
        return 'outline';
    }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Команды (v2)</h1>
          <p className="text-sm text-muted-foreground">
            Управление командами и доступом
          </p>
        </div>
        
        <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Создать команду
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Создать новую команду</DialogTitle>
            </DialogHeader>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="teamName">Название</Label>
                <Input
                  id="teamName"
                  value={newTeamName}
                  onChange={(e) => setNewTeamName(e.target.value)}
                  placeholder="Например: Development Team"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="teamDescription">Описание (необязательно)</Label>
                <Input
                  id="teamDescription"
                  value={newTeamDescription}
                  onChange={(e) => setNewTeamDescription(e.target.value)}
                  placeholder="Описание команды"
                />
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setCreateDialogOpen(false)}>
                Отмена
              </Button>
              <Button onClick={handleCreateTeam} disabled={loadingAction || !newTeamName.trim()}>
                {loadingAction ? 'Создание...' : 'Создать'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Ваши команды</CardTitle>
          <CardDescription>
            Команды, в которых вы участвуете
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {[1, 2, 3].map((i) => (
                <Card key={i}>
                  <CardHeader className="pb-2">
                    <Skeleton className="h-4 w-3/4" />
                  </CardHeader>
                  <CardContent>
                    <Skeleton className="h-3 w-1/2" />
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : teams.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <Users className="mx-auto h-12 w-12 mb-4 opacity-50" />
              <p>Нет команд</p>
              <p className="text-sm">Создайте первую команду для начала работы</p>
            </div>
          ) : (
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {teams.map((team) => (
                <Card 
                  key={team.id} 
                  className="hover:shadow-md transition-shadow cursor-pointer"
                  onClick={() => handleViewTeam(team)}
                >
                  <CardHeader className="pb-2">
                    <div className="flex items-start justify-between">
                      <div className="flex items-center gap-2">
                        <Users className="h-4 w-4 text-muted-foreground" />
                        <CardTitle className="text-base">{team.name}</CardTitle>
                      </div>
                      {team.ownerId && (
                        <Badge variant="outline" className="text-xs">
                          Owner
                        </Badge>
                      )}
                    </div>
                  </CardHeader>
                  <CardContent>
                    {team.description ? (
                      <p className="text-sm text-muted-foreground line-clamp-2">
                        {team.description}
                      </p>
                    ) : (
                      <p className="text-sm text-muted-foreground">Нет описания</p>
                    )}
                    <div className="flex items-center justify-between mt-3 text-xs text-muted-foreground">
                      <span>Создано: {new Date(team.createdAt).toLocaleDateString('ru-RU')}</span>
                      <Button variant="ghost" size="sm" className="h-6 px-2">
                        <Edit className="h-3 w-3" />
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Team Details Dialog */}
      <Dialog open={detailsDialogOpen} onOpenChange={setDetailsDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{selectedTeam?.name}</DialogTitle>
          </DialogHeader>

          {selectedTeam && (
            <div className="space-y-6">
              <div className="text-sm text-muted-foreground">
                <p>Описание: {selectedTeam.description || 'Нет описания'}</p>
                <p>Создано: {new Date(selectedTeam.createdAt).toLocaleString('ru-RU')}</p>
              </div>

              <div>
                <h3 className="text-lg font-semibold mb-3 flex items-center gap-2">
                  <Shield className="h-4 w-4" />
                  Участники
                </h3>
                
                {members.length === 0 ? (
                  <Alert>
                    <AlertDescription>
                      Пока нет участников. Добавьте участников команды.
                    </AlertDescription>
                  </Alert>
                ) : (
                  <div className="space-y-2">
                    {members.map((member) => (
                      <div
                        key={member.id}
                        className="flex items-center justify-between p-3 border rounded-lg"
                      >
                        <div>
                          <p className="font-medium">User: {member.userId}</p>
                          <p className="text-xs text-muted-foreground">
                            Создано: {new Date(member.createdAt).toLocaleDateString('ru-RU')}
                          </p>
                        </div>
                        <Badge variant={getRoleBadgeVariant(member.role)}>
                          {member.role}
                        </Badge>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <div className="border-t pt-4">
                <h3 className="text-lg font-semibold mb-3">Действия</h3>
                <div className="flex gap-2">
                  <Button variant="outline" className="flex-1">
                    <Plus className="mr-2 h-4 w-4" />
                    Добавить участника
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={handleDeleteTeam}
                    disabled={loadingAction}
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Удалить команду
                  </Button>
                </div>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
